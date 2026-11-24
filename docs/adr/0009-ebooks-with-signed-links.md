# 0009. E-books stored in the database and served through signed links

## Context

E-books are borrowed like printed books, but the file must only be readable while the loan is active. The free host has no blob storage, and files in the site folder can be removed by Web Deploy.

## Decision

- Files go through an `IFileStorage` abstraction. The current implementation stores them in a `StoredFiles` table; the demo catalog holds about 20 public-domain EPUBs, a few megabytes each, well within the 1 GB database.
- The reader asks for a link and gets `/ebooks/{loanId}/content?expires=…&signature=…`. The signature is HMAC-SHA256 over the loan id and the expiry, with a key derived from the JWT key, and is compared in constant time. Links live for 10 minutes, and the endpoint also checks that the loan is still active.
- Uploads are accepted only when the extension and the magic bytes agree (EPUB is a ZIP, PDF starts with `%PDF`).

## Consequences

- The file endpoint needs no JWT, so `<iframe>`, epub.js and download managers work, and leaked links stop working quickly.
- Storing files in the database is simple and survives redeploys, but it does not scale to large collections; moving to Azure Blob Storage or S3 only means another `IFileStorage` implementation.
