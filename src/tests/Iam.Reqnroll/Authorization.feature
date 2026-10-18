Feature: Authorization API contract
Permission assignment and evaluation ride the real Hosts.HQ HTTP surface (identity/authorization spec delta mapped 1:1).
授權 API 契約：經 Hosts.HQ HTTP 面 1:1 覆蓋 identity/authorization spec 場景。

  Scenario: Assign role
    Given the tenant "authz-a" is readied named "Authz A Freight" with the initial organization "Authz A HQ"
    And a user "alice-role" is provisioned in tenant "authz-a"
    And the role "reader-lab" granting "documents:read" is provisioned in tenant "authz-a"
    When the administrator assigns that role to that user in the tenant
    Then the system records the assignment

  Scenario: Cross-tenant assignment rejected
    Given the tenant "authz-b" is readied named "Authz B Freight" with the initial organization "Authz B HQ"
    And a user "bruno-role" is provisioned in tenant "authz-b"
    And the tenant "authz-c" is readied named "Authz C Freight" with the initial organization "Authz C HQ"
    And the role "reader-lab" granting "documents:read" is provisioned in tenant "authz-c"
    When the administrator assigns the "authz-c" role to the "authz-b" user in tenant "authz-b"
    Then the system rejects the assignment and records nothing

  Scenario: Granted permission allows
    Given the tenant "authz-d" is readied named "Authz D Freight" with the initial organization "Authz D HQ"
    And a user "cleo-role" is provisioned in tenant "authz-d"
    And the role "reader-lab" granting "documents:read" is provisioned in tenant "authz-d"
    And the role is assigned to the user
    When the system evaluates "documents:read" for that user in tenant "authz-d"
    Then the decision is allow

  Scenario: Missing grant denies
    Given the tenant "authz-e" is readied named "Authz E Freight" with the initial organization "Authz E HQ"
    And a user "dima-role" is provisioned in tenant "authz-e"
    And the role "reader-lab" granting "documents:read" is provisioned in tenant "authz-e"
    And the role is assigned to the user
    When the system evaluates "billing:write" for that user in tenant "authz-e"
    Then the decision is deny

  Scenario: Cross-tenant evaluation denied
    Given the tenant "authz-f" is readied named "Authz F Freight" with the initial organization "Authz F HQ"
    And a user "enzo-role" is provisioned in tenant "authz-f"
    And the role "reader-lab" granting "documents:read" is provisioned in tenant "authz-f"
    And the role is assigned to the user
    And the tenant "authz-g" is readied named "Authz G Freight" with the initial organization "Authz G HQ"
    When the system evaluates "documents:read" for the "authz-f" user in tenant "authz-g"
    Then the decision is deny

  Scenario: Revoked permission denied after propagation
    Given the tenant "authz-h" is readied named "Authz H Freight" with the initial organization "Authz H HQ"
    And a user "fay-role" is provisioned in tenant "authz-h"
    And the role "reader-lab" granting "documents:read" is provisioned in tenant "authz-h"
    And the role is assigned to the user
    When the administrator revokes the assignment and propagation completes
    Then the decision for "documents:read" is deny
