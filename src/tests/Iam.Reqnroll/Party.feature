# Maps the identity-party spec delta 1:1 (task 3.3). Party identity has NO HTTP surface by design
# (guarded structural exception — exact-identifier reach only), so the steps drive the real
# Application use cases through the hosted app's DI over wired Postgres; the denial scenario
# additionally rides the real HTTP server as a route-absence sentinel. Login disabling (preserved
# history scenario) rides the user lifecycle (tasks 4.1–4.2) and binds when that surface lands.
# 映射 identity-party spec 一對一（task 3.3）：Party 定制守衛結構例外、無 HTTP 面；步驟驅動宿主真實
# DI＋Application use case＋wired Postgres；「拒絕」場景以真實 HTTP 驗證路由缺席；登入停用屬 4.1–4.2。

Feature: Party identity contract

  Scenario: Provision new party
    Given no party has ever been resolved with the unique identifier tuple
    When the operator provisions a party "Party One" born 1990-05-01 with that identifier tuple
    Then the system creates the party

  Scenario: Exact match binds without duplication
    Given a party exists carrying the unique identifier tuple
    When the operator provisions a party "Party Two" born 1985-03-12 with the same identifier tuple
    Then the system binds to the existing party and creates no duplicate

  Scenario: Near match creates new party
    Given a party exists carrying the unique identifier tuple
    When the operator provisions a party "Ada Chen" born 1990-03-01 with a different identifier tuple
    Then the system creates a new party instead of merging

  Scenario: Traversal denied
    Given the tenant "guarded-a" is prepared named "Guarded Freight" with the initial organization "Guarded HQ"
    And a party exists carrying the unique identifier tuple
    When the caller scoped to tenant "guarded-a" requests party listings
    Then the system denies the request because parties cannot be listed or traversed

  Scenario: Exact-match bind allowed
    Given the tenant "host-b" is prepared named "Host B Freight" with the initial organization "Host B HQ"
    And a party carrying the unique identifier tuple plays the "employee" role in tenant "host-b"
    And the tenant "host-a" is prepared named "Host A Freight" with the initial organization "Host A HQ"
    When the caller registers that identifier tuple as an "employee" in tenant "host-a"
    Then the system binds the new tenant role to that party without exposing other data

  Scenario: Deactivation preserves history
    Given a party carrying the unique identifier tuple plays the "employee" role in tenant "hist-a"
    And that party holds consent "treatment-data" granted in tenant "hist-a"
    When the "employee" role of the party in tenant "hist-a" is deactivated
    Then the party keeps its identifier tuple
    And its consent history remains intact

  Scenario: One human, multiple roles
    Given a party carrying the unique identifier tuple plays the "employee" role in tenant "multi-a"
    When the caller registers that identifier tuple as an "examinee" in tenant "multi-a"
    Then the "examinee" role is added to the same party
    And deactivating each of the party's roles succeeds

  Scenario: Consent recorded
    Given a party exists carrying the unique identifier tuple
    When the caller records consent "treatment-data" granted by that party in tenant "cons-a"
    Then the system stores a timestamped immutable consent record

  Scenario: Revocation appends, original retained
    Given a party exists carrying the unique identifier tuple
    And that party holds consent "treatment-data" granted in tenant "rev-a"
    When the caller records that the party revokes the consent "treatment-data" in tenant "rev-a"
    Then the system appends a revocation record and retains the original
