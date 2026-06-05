# BankLite Load Test

Target: https://banklite.ca  
Tool: k6  
Scenario: Public web benchmark  
Peak load: 1,000 virtual users  
Duration: 7m30s  
Requests: 533,120  
Throughput: 1,184 req/s  
p50: 195ms  
p95: 581ms  
p99: 844ms  
Failure rate: 0.00%  
Successful journeys: 99.99%

This benchmark covers public pages and static assets: landing, login, register, CSS, and JavaScript.

## Summary

```text
banklite_page_load
  p(95)<800 p(95)=580.99206

banklite_successful_journey
  rate>0.99 rate=99.99%

http_req_duration
  p(50)<250 p(50)=195.37ms
  p(95)<1000 p(95)=580.99ms
  p(99)<2000 p(99)=843.65ms

http_req_failed
  rate<0.01 rate=0.00%

checks_total: 533120
checks_succeeded: 99.99%
checks_failed: 0.00%
http_reqs: 533120, 1184.687997/s
iterations: 106654
vus_max: 1001
data_received: 7.8 GB
data_sent: 56 MB
```
