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
    http.get(`${BASE_URL}/api/v1/categories`, { headers });
  }
}

// ───── Custom Metrics ─────
const categoryListDuration = new Trend("category_list_duration", true);
const categoryGetDuration = new Trend("category_get_by_id_duration", true);
const categoryCreateDuration = new Trend("category_create_duration", true);
const categoryUpdateDuration = new Trend("category_update_duration", true);
const categoryDeleteDuration = new Trend("category_delete_duration", true);
const errorRate = new Rate("errors");
const requestCount = new Counter("total_requests");

// ───── Test Profile ─────
// Override via: k6 run --env PROFILE=stress categories.js
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

  // 1. GET /api/v1/categories (list) — also extract an ID for later use
  const categoryId = listCategories(headers);
  sleep(0.5);

  // 2. GET /api/v1/categories?page=1&limit=5&sort=name_asc (filtered)
  listCategoriesFiltered(headers);
  sleep(0.5);

  // 3. GET /api/v1/categories/:id (single)
  if (categoryId) {
    getCategory(headers, categoryId);
    sleep(0.5);
  }

  // 4. CRUD cycle (only if JWT is available)
  if (hasAuth) {
    crudCycle(authHeaders);
    sleep(0.5);
  }
}

// ───── Test Functions ─────

function listCategories(headers) {
  const res = http.get(`${BASE_URL}/api/v1/categories`, { headers, tags: { name: "GET /categories" } });
  requestCount.add(1);
  categoryListDuration.add(res.timings.duration);
  const success = check(res, {
    "list: status is 200": (r) => r.status === 200,
    "list: has items array": (r) => {
      const body = r.json();
      return body && body.data !== undefined;
    },
  });
  errorRate.add(!success);

  // Extract a real ID from the response for subsequent requests
  let categoryId = null;
  if (res.status === 200) {
    try {
      const body = res.json();
      if (body.data && body.data.length > 0) {
        categoryId = body.data[0].id;
      }
    } catch (_) {}
  }
  return categoryId;
}

function listCategoriesFiltered(headers) {
  const res = http.get(`${BASE_URL}/api/v1/categories?page=1&limit=5&sort=name_asc`, {
    headers,
    tags: { name: "GET /categories?filtered" },
  });
  requestCount.add(1);
  categoryListDuration.add(res.timings.duration);
  const success = check(res, {
    "filtered: status is 200": (r) => r.status === 200,
  });
  errorRate.add(!success);
}

function getCategory(headers, categoryId) {
  const res = http.get(`${BASE_URL}/api/v1/categories/${categoryId}`, {
    headers,
    tags: { name: "GET /categories/:id" },
  });
  requestCount.add(1);
  categoryGetDuration.add(res.timings.duration);
  const success = check(res, {
    "getById: status is 200 or 404": (r) => r.status === 200 || r.status === 404,
  });
  errorRate.add(!success);
}

function crudCycle(headers) {
  // CREATE
  const uniqueCode = `K6_${Date.now()}_${Math.floor(Math.random() * 10000)}`;
  const createRes = http.post(
    `${BASE_URL}/api/v1/categories`,
    JSON.stringify({ code: uniqueCode, name: `Load Test ${uniqueCode}` }),
    { headers, tags: { name: "POST /categories" } }
  );
  requestCount.add(1);
  categoryCreateDuration.add(createRes.timings.duration);
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
    `${BASE_URL}/api/v1/categories/${id}`,
    JSON.stringify({ name: `Updated ${uniqueCode}` }),
    { headers, tags: { name: "PUT /categories/:id" } }
  );
  requestCount.add(1);
  categoryUpdateDuration.add(updateRes.timings.duration);
  const updateOk = check(updateRes, {
    "update: status is 200": (r) => r.status === 200,
  });
  errorRate.add(!updateOk);

  // DELETE
  const deleteRes = http.del(`${BASE_URL}/api/v1/categories/${id}`, null, {
    headers,
    tags: { name: "DELETE /categories/:id" },
  });
  requestCount.add(1);
  categoryDeleteDuration.add(deleteRes.timings.duration);
  const deleteOk = check(deleteRes, {
    "delete: status is 204": (r) => r.status === 204,
  });
  errorRate.add(!deleteOk);
}
