# Arcane

> *Your thoughts. Yours alone.*

Arcane is a private, cross-platform encrypted diary application built to provide a secure, native space for personal writing. It relies on application-layer encryption to ensure that data remains entirely confidential, even if the underlying database is compromised.

---

## Architecture & Security

Arcane uses a zero-knowledge architecture. No unencrypted text ever hits the disk, and your master key never persists in memory.

- **Key Derivation (KDF):** Uses **Argon2id** (configured across iterations, memory cost, and parallelism thresholds) to stretch user passwords into a high-entropy 256-bit cryptographic key.
- **Symmetric Encryption:** Uses **AES-256-GCM** to handle content blocks. Every single entry payload is bundled with a uniquely generated, non-reused cryptographic nonce.
- **Data Persistence:** Built on **EF Core 10** managing a local **SQLite** target. Plaintext contents are processed completely in-memory via `ReadOnlySpan<byte>` blocks before serialization.

---

## Core Stack

- **Runtime:** .NET 10 (C# 14)
- **UI Framework:** Avalonia UI 12 (Native Wayland support)
- **Database:** SQLite + Entity Framework Core 10
- **Testing:** xUnit v3

---

## Repository Structure

```text
├── Arcane.App/       # Avalonia UI presentation layer (MVVM)
├── Arcane.Core/      # Domain models, database context, and crypto services
└── Arcane.Tests/     # xUnit test suites validating cryptographic roundtrips
```

