import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Rate, Counter } from "k6/metrics";
import {
  BASE_URL,
  getPublicHeaders,
  getAuthHeaders,
  smokeTest,
  averageLoad,
  stressTest,
  spikeTest,
  soakTest,
} from "./config.js";

// ───── Warmup (runs once before test, not counted in metrics) ─────
export function setup() {
  const headers = getPublicHeaders();
  for (let i = 0; i < 3; i++) {
    http.get(`${BASE_URL}/api/v1/sub-categories`, { headers });
  }
}

// ───── Custom Metrics ─────
const subCategoryListDuration = new Trend("sub_category_list_duration", true);
const subCategoryGetDuration = new Trend("sub_category_get_by_id_duration", true);
const subCategoryCreateDuration = new Trend("sub_category_create_duration", true);
const subCategoryUpdateDuration = new Trend("sub_category_update_duration", true);
const subCategoryDeleteDuration = new Trend("sub_category_delete_duration", true);
const errorRate = new Rate("errors");
const requestCount = new Counter("total_requests");

// ───── Test Profile ─────
const profile = __ENV.PROFILE || "average";

const profiles = {
  smoke: smokeTest,
  average: averageLoad,
  stress: stressTest,
  spike: spikeTest,
  soak: soakTest,
};

export const options = profiles[profile] || profiles.average;

// ───── Scenarios ─────

export default function () {
  const headers = getPublicHeaders();
  const authHeaders = getAuthHeaders();
  const hasAuth = !!__ENV.JWT_TOKEN;

  // 1. GET /api/v1/sub-categories (list) — also extract IDs for later use
  const { subCategoryId, categoryId } = listSubCategories(headers);
  sleep(0.5);

  // 2. GET /api/v1/sub-categories/by-category/:categoryId
  if (categoryId) {
    listByCategoryId(headers, categoryId);
    sleep(0.5);
  }

  // 3. GET /api/v1/sub-categories/:id (single)
  if (subCategoryId) {
    getSubCategory(headers, subCategoryId);
    sleep(0.5);
  }

  // 4. CRUD cycle (only if JWT is available)
  if (hasAuth) {
    crudCycle(authHeaders, categoryId || 1);
    sleep(0.5);
  }
}

// ───── Test Functions ─────

function listSubCategories(headers) {
  const res = http.get(`${BASE_URL}/api/v1/sub-categories`, {
    headers,
    tags: { name: "GET /sub-categories" },
  });
  requestCount.add(1);
  subCategoryListDuration.add(res.timings.duration);
  const success = check(res, {
    "list: status is 200": (r) => r.status === 200,
    "list: has data": (r) => {
      const body = r.json();
      return body && body.data !== undefined;
    },
  });
  errorRate.add(!success);

  // Extract a real ID and categoryId from the response for subsequent requests
  let subCategoryId = null;
  let categoryId = null;
  if (res.status === 200) {
    try {
      const body = res.json();
      if (body.data && body.data.length > 0) {
        subCategoryId = body.data[0].id;
        categoryId = body.data[0].categoryId;
      }
    } catch (_) {}
  }
  return { subCategoryId, categoryId };
}

function listByCategoryId(headers, categoryId) {
  const res = http.get(`${BASE_URL}/api/v1/sub-categories/by-category/${categoryId}`, {
    headers,
    tags: { name: "GET /sub-categories/by-category/:id" },
  });
  requestCount.add(1);
  subCategoryListDuration.add(res.timings.duration);
  const success = check(res, {
    "byCategory: status is 200": (r) => r.status === 200,
  });
  errorRate.add(!success);
}

function getSubCategory(headers, subCategoryId) {
  const res = http.get(`${BASE_URL}/api/v1/sub-categories/${subCategoryId}`, {
    headers,
    tags: { name: "GET /sub-categories/:id" },
  });
  requestCount.add(1);
  subCategoryGetDuration.add(res.timings.duration);
  const success = check(res, {
    "getById: status is 200 or 404": (r) => r.status === 200 || r.status === 404,
  });
  errorRate.add(!success);
}

function crudCycle(headers, categoryId) {
  // CREATE (needs a valid categoryId)
  const uniqueCode = `K6S_${Date.now()}_${Math.floor(Math.random() * 10000)}`;
  const createRes = http.post(
    `${BASE_URL}/api/v1/sub-categories`,
    JSON.stringify({ code: uniqueCode, name: `Load Test Sub ${uniqueCode}`, categoryId: categoryId }),
    { headers, tags: { name: "POST /sub-categories" } }
  );
  requestCount.add(1);
  subCategoryCreateDuration.add(createRes.timings.duration);
  const createOk = check(createRes, {
    "create: status is 201": (r) => r.status === 201,
  });
  errorRate.add(!createOk);

  if (createRes.status !== 201) return;

  const created = createRes.json();
  const id = created.data?.id;
  if (!id) return;

  // UPDATE
  const updateRes = http.put(
    `${BASE_URL}/api/v1/sub-categories/${id}`,
    JSON.stringify({ name: `Updated Sub ${uniqueCode}` }),
    { headers, tags: { name: "PUT /sub-categories/:id" } }
  );
  requestCount.add(1);
  subCategoryUpdateDuration.add(updateRes.timings.duration);
  const updateOk = check(updateRes, {
    "update: status is 200": (r) => r.status === 200,
  });
  errorRate.add(!updateOk);

  // DELETE
  const deleteRes = http.del(`${BASE_URL}/api/v1/sub-categories/${id}`, null, {
    headers,
    tags: { name: "DELETE /sub-categories/:id" },
  });
  requestCount.add(1);
  subCategoryDeleteDuration.add(deleteRes.timings.duration);
  const deleteOk = check(deleteRes, {
    "delete: status is 204": (r) => r.status === 204,
  });
  errorRate.add(!deleteOk);
}
