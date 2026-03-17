// load-test.js
import http from "k6/http";

export const options = {
  scenarios: {
    fixed_count: {
      executor: "shared-iterations",
      vus: 200,
      iterations: 100000,
      maxDuration: "30m",
    },
  },
};

export default function () {
  const payload = JSON.stringify({ payload: "test" });
  const params = { headers: { "Content-Type": "application/json" } };
  http.post("http://localhost:5080/api/forms/send", payload, params);
}
