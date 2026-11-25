import http from "k6/http";
import { check, group, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5080";
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || "admin@library.local";
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || "Admin@123";

export const options = {
  scenarios: {
    catalog: {
      executor: "ramping-vus",
      exec: "browseCatalog",
      startVUs: 0,
      stages: [
        { duration: "15s", target: 30 },
        { duration: "30s", target: 30 },
        { duration: "10s", target: 0 },
      ],
    },
    members: {
      executor: "constant-vus",
      exec: "memberSession",
      vus: 5,
      duration: "55s",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    "http_req_duration{scenario:catalog}": ["p(95)<300"],
    "http_req_duration{scenario:members}": ["p(95)<1000"],
  },
};

const jsonHeaders = { headers: { "Content-Type": "application/json" } };

export function setup() {
  const login = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }), jsonHeaders);
  const token = login.json("accessToken");
  const auth = { headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` } };

  for (let i = 1; i <= 50; i++) {
    http.post(`${BASE_URL}/api/v1/books`, JSON.stringify({ title: `Load Test Book ${i}`, author: `Author ${i % 10}`, description: "Seeded by k6", genre: "Load Test", copies: 2 }), auth);
  }
}

export function browseCatalog() {
  group("browse catalog", () => {
    const page = Math.floor(Math.random() * 5) + 1;
    const list = http.get(`${BASE_URL}/api/v1/books?page=${page}&pageSize=10`);
    check(list, { "catalog 200": (response) => response.status === 200 });

    const search = http.get(`${BASE_URL}/api/v1/books?search=Author%20${page}`);
    check(search, { "search 200": (response) => response.status === 200 });
  });

  sleep(1);
}

export function memberSession() {
  group("member session", () => {
    const email = `load-${__VU}-${__ITER}-${Date.now()}@tests.com`;
    const register = http.post(
      `${BASE_URL}/api/v1/auth/register`,
      JSON.stringify({ name: "Load Tester", birthDate: "1995-05-10", phone: "555-0100", email, password: "Password@123" }),
      jsonHeaders,
    );
    check(register, { "register 201": (response) => response.status === 201 });

    const login = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({ email, password: "Password@123" }), jsonHeaders);
    check(login, { "login 200": (response) => response.status === 200 });

    const me = http.get(`${BASE_URL}/api/v1/auth/me`, { headers: { Authorization: `Bearer ${login.json("accessToken")}` } });
    check(me, { "me 200": (response) => response.status === 200 });

    const refresh = http.post(`${BASE_URL}/api/v1/auth/refresh`, JSON.stringify({ refreshToken: login.json("refreshToken") }), jsonHeaders);
    check(refresh, { "refresh 200": (response) => response.status === 200 });
  });

  sleep(1);
}
