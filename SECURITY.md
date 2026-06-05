# Security Policy

## Supported Version

| Version | Supported |
|---|---|
| 1.0.x | Yes |

## Reporting a Vulnerability

Do not open a public issue for suspected vulnerabilities, leaked credentials, authentication bypasses, or production exposure.

Report security issues privately to the repository owner with:

- affected endpoint or component
- reproduction steps
- expected impact
- relevant logs or screenshots with secrets removed

I will review valid reports as quickly as possible and prioritize fixes based on severity and exploitability.

## Scope

In scope:

- authentication and authorization bugs
- token/session handling issues
- CSRF, XSS, injection, and request validation issues
- sensitive data exposure
- infrastructure or deployment misconfiguration that affects BankLite

Out of scope:

- denial-of-service testing against production without permission
- social engineering
- automated spam or credential stuffing
- reports requiring access to secrets, private keys, or accounts you do not own
