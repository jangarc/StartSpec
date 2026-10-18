Feature: Tenant API contract
  # Maps the identity-tenant spec delta 1:1 (task 2.4) over the real HTTP surface of Hosts.HQ.

  Scenario: Provision new tenant
    Given I provision a tenant "prov-a" named "Prov Freight" with the initial organization "Prov HQ"
    Then the system creates the tenant "prov-a" with its initial organization and returns the tenant identifier

  Scenario: Duplicate tenant rejected
    Given I provision a tenant "dup-a" named "Dup Freight" with the initial organization "Dup HQ"
    When I provision a tenant "dup-a" named "Dup Freight 2" with the initial organization "Dup HQ 2"
    Then the system rejects the request with a conflict error
    And the tenant "dup-a" keeps its single original organization

  Scenario: Resolve active tenant
    Given I provision a tenant "resolve-a" named "Resolve Freight" with the initial organization "Resolve HQ"
    When the caller scoped to tenant "resolve-a" lists organizations
    Then the system resolves tenant "resolve-a" as the active tenant for the operation

  Scenario: Missing tenant scope rejected
    When the caller without a tenant scope requests organization listings
    Then the system rejects the request without executing business logic

  Scenario: Cross-tenant read denied
    Given I provision a tenant "iso-a" named "Iso Freight" with the initial organization "Iso HQ"
    And I provision a tenant "iso-b" named "Iso Bolt" with the initial organization "Iso B HQ"
    When the caller scoped to tenant "iso-b" reads the last provisioned organization of tenant "iso-a"
    Then the system denies access as if the record does not exist

  Scenario: List excludes other tenants
    Given I provision a tenant "list-a" named "List Freight" with the initial organization "List HQ"
    And I provision a tenant "list-b" named "List Bolt" with the initial organization "List B HQ"
    When the caller scoped to tenant "list-a" lists organizations
    Then the system returns only records owned by tenant "list-a"

  Scenario: Cross-tenant read looks like not-found
    Given I provision a tenant "uni-a" named "Uni Freight" with the initial organization "Uni HQ"
    And I provision a tenant "uni-b" named "Uni Bolt" with the initial organization "Uni B HQ"
    When the caller scoped to tenant "uni-b" reads the last provisioned organization of tenant "uni-a"
    And the caller scoped to tenant "uni-b" also requests a truly unknown organization
    Then the system responds exactly as if the record does not exist

  Scenario: Lists expose no totals
    Given I provision a tenant "page-a" named "Page Freight" with the initial organization "Page HQ"
    When the caller scoped to tenant "page-a" lists organizations
    Then the system returns a has-next indicator without exact totals
