// Thresholds de performance para todos os testes
export const THRESHOLDS = {
  // Tempo de resposta percentil 95 deve ser menor que 500ms
  'http_req_duration': ['p(95)<500'],
  
  // Taxa de erro deve ser menor que 1%
  'http_req_failed': ['rate<0.01'],
  
  // Pelo menos 100 requests por segundo
  'http_reqs': ['rate>100'],
};

// Thresholds mais rigorosos para endpoints críticos
export const CRITICAL_ENDPOINT_THRESHOLDS = {
  'http_req_duration': ['p(95)<300', 'p(99)<500'],
  'http_req_failed': ['rate<0.005'],
  'http_reqs': ['rate>200'],
};

// Thresholds relaxados para testes de stress
export const STRESS_THRESHOLDS = {
  'http_req_duration': ['p(95)<2000', 'p(99)<5000'],
  'http_req_failed': ['rate<0.05'],
  'http_reqs': ['rate>50'],
};

// Thresholds para testes de endurance (longa duração)
export const ENDURANCE_THRESHOLDS = {
  'http_req_duration': ['p(95)<800', 'avg<400'],
  'http_req_failed': ['rate<0.02'],
  'http_reqs': ['rate>80'],
};
