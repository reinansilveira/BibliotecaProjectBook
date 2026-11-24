# 0007. Free hosting on MonsterASP.NET and GitHub Pages

## Context

The project should have a public link that a recruiter can open, at no cost and without a credit card. Azure for Students was the first choice, but it only accepts schools whose email domain Microsoft has registered, and the author's school is not one of them.

## Decision

- **API and database on MonsterASP.NET (free plan).** It runs ASP.NET Core 10 on IIS and includes a SQL Server database, so the EF Core migrations, the Dapper report and the stored procedure run unchanged. No credit card is required.
- **Front end on GitHub Pages.** The Angular build is static, so it needs no server. `404.html` is a copy of `index.html`, which makes deep links such as `/books/42` work, and the build sets `--base-href` to the repository name.
- **Continuous deployment** with GitHub Actions, triggered only after CI passes on `main`. The API is published for IIS and sent with **Web Deploy**, then a smoke test calls `/health` until it answers. The front end is built with the API address and published with the official Pages actions.
- **Secrets stay on the host.** The connection string, the JWT key and the administrator account are environment variables in the MonsterASP control panel; GitHub only stores the Web Deploy credentials.
- The front end and the API live on different hosts, so the API allows the GitHub Pages origin through **CORS**, and the Angular build receives the API address at build time.
- RabbitMQ has no free managed offer that fits, so messaging is turned off by configuration in the cloud; the outbox keeps recording events.

## Consequences

- Zero cost, with clear limits: 256 MB of memory, a 1 GB database, a datacenter in Europe and a cold start after the site has been idle.
- HTTPS for the free subdomain is activated by MonsterASP support on request. It is required, because a page served over HTTPS cannot call an API over HTTP.
- The hosting is not tied to the code: the API is a standard ASP.NET Core app and the Docker image still works, so moving to Azure, AWS or a container host later only changes the deploy workflow.
