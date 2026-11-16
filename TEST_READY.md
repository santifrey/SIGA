# E2E Test Suite Ready

## Test Runner
- Command: `dotnet test tests/SIGA.E2ETests/SIGA.E2ETests.csproj --logger "console;verbosity=normal"`
- Expected: all 391 tests pass with exit code 0

## Coverage Summary
| Tier | Count | Description |
|------|------:|-------------|
| 1. Feature Coverage | 170 | 5 tests per feature across all 34 features |
| 2. Boundary & Corner | 170 | 5 boundary tests per feature across all 34 features |
| 3. Cross-Feature | 34 | Pairwise combinatorial interactions between modules |
| 4. Real-World Application | 17 | Realistic end-to-end multi-actor business workflows |
| **Total** | **391** | Complete opaque-box E2E test suite |

## Feature Checklist
| # | Feature | Tier 1 | Tier 2 | Tier 3 | Tier 4 |
|---|---------|:------:|:------:|:------:|:------:|
| 1 | Single-Role User Model | 5 | 5 | ✓ | ✓ |
| 2 | Role-Permission Model | 5 | 5 | ✓ | ✓ |
| 3 | Permissions Catalog & Seeding | 5 | 5 | ✓ | ✓ |
| 4 | Dynamic Permission Authorization | 5 | 5 | ✓ | ✓ |
| 5 | Single-Role & Permission JWT | 5 | 5 | ✓ | ✓ |
| 6 | Roles & Permissions CRUD API | 5 | 5 | ✓ | ✓ |
| 7 | Employees CRUD API | 5 | 5 | ✓ | ✓ |
| 8 | Complete User Profile API | 5 | 5 | ✓ | ✓ |
| 9 | Omission of Notifications | 5 | 5 | — | ✓ |
| 10 | Security & Auth Unit Tests | 5 | 5 | — | — |
| 11 | Unified MovimientoComercial Entity | 5 | 5 | ✓ | ✓ |
| 12 | Commercial Financial Rules | 5 | 5 | ✓ | ✓ |
| 13 | Commercial Workflow Transitions | 5 | 5 | ✓ | ✓ |
| 14 | Non-Terminal Rejection & Correction | 5 | 5 | ✓ | ✓ |
| 15 | MIX Items & Stock Decomposition | 5 | 5 | ✓ | ✓ |
| 16 | New Client Contract Enforcement | 5 | 5 | ✓ | ✓ |
| 17 | Traceability & Approval History | 5 | 5 | ✓ | ✓ |
| 18 | Finalization Stock Impact | 5 | 5 | ✓ | ✓ |
| 19 | Relocated Reference Prices API | 5 | 5 | ✓ | ✓ |
| 20 | Commercial Unit Tests | 5 | 5 | — | — |
| 21 | Structured Row & Lot Backend | 5 | 5 | ✓ | ✓ |
| 22 | Structured Row & Lot Frontend | 5 | 5 | ✓ | ✓ |
| 23 | Plantations Unit Tests | 5 | 5 | ✓ | — |
| 24 | Sidebar 5 Collapsible Sections | 5 | 5 | ✓ | ✓ |
| 25 | Sidebar Persistence & Auto-Expansion | 5 | 5 | ✓ | ✓ |
| 26 | Frontend Route Guards & Perm Filtering | 5 | 5 | ✓ | ✓ |
| 27 | Topbar Profile Navigation & No Bell | 5 | 5 | ✓ | ✓ |
| 28 | Deactivation Prohibition Symbol | 5 | 5 | ✓ | ✓ |
| 29 | User Profile View (#perfil) | 5 | 5 | ✓ | ✓ |
| 30 | Employees Management View (#empleados) | 5 | 5 | ✓ | ✓ |
| 31 | Roles & Permissions View (#roles) | 5 | 5 | ✓ | ✓ |
| 32 | Stock View Bug Fix (#stock) | 5 | 5 | ✓ | ✓ |
| 33 | Standalone Reference Prices View | 5 | 5 | ✓ | ✓ |
| 34 | Unified Commercial Movements View | 5 | 5 | ✓ | ✓ |
