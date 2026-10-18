# Maps the identity/user-auth spec delta 1:1 (task 4.4) over the real HTTP surface of Hosts.HQ:
# user provisioning rides the tenant-scoped user route; sign-in and refresh carry the tenant in their
# bodies. Failure shapes keep the Application contract byte-level: ONE generic sign-in body for all
# credentials rejections, ONE generic refresh body for all refresh rejections.
# 映射 identity/user-auth spec 一對一（task 4.4）：使用者建立吃租戶範圍路由；登入與刷新以身分置於本體；
# 失敗形保持位元級：登入拒絕單一本體、刷新拒絕單一本體。

Feature: UserAuth API contract

  Scenario: Provision user
    Given the tenant "auth-a" is provisioned named "Auth A Freight" with the initial organization "Auth A HQ"
    When I provision a user "ada" displayed "Ada Chen" born 1990-05-01 with the identifier "auth-a-original" in tenant "auth-a"
    Then the system creates the user and returns the user and party identifiers

  Scenario: Exact identifier match binds existing party
    Given the tenant "auth-b" is provisioned named "Auth B Freight" with the initial organization "Auth B HQ"
    And a user "bruno" is provisioned displayed "Bruno Lin" born 1985-03-12 in tenant "auth-b"
    When I provision a user "clara" displayed "Clara Wong" born 1992-10-20 with that user's identifier in tenant "auth-b"
    Then the new user binds the existing user's party

  Scenario: Duplicate login rejected
    Given the tenant "dup-u" is provisioned named "Dup U Freight" with the initial organization "Dup U HQ"
    And a user "dave" is provisioned displayed "Dave First" born 1990-01-01 in tenant "dup-u"
    When I provision a user "dave" displayed "Dave Second" born 1991-02-02 with a fresh identifier in tenant "dup-u"
    Then the system rejects the provisioning with a conflict error

  Scenario: Successful sign-in
    Given the operator "eva" with the password "Correct-Operator-0000" exists in tenant "sign-a" named "Sign A Freight" with the initial organization "Sign A HQ"
    When "eva" signs in to tenant "sign-a" with that password
    Then the system returns the operator user identity and a fresh session pair

  Scenario: Invalid credentials rejected generically
    Given the operator "frank" with the password "Correct-Operator-0000" exists in tenant "sign-b" named "Sign B Freight" with the initial organization "Sign B HQ"
    When "frank" attempts sign-in to tenant "sign-b" with a wrong password
    And an unknown login "ghost-login" attempts sign-in to tenant "sign-b"
    Then both sign-in failures share the identical generic rejection body

  Scenario: Unknown tenant rejected indistinguishably
    Given the operator "gina" with the password "Correct-Operator-0000" exists in tenant "sign-c" named "Sign C Freight" with the initial organization "Sign C HQ"
    When "gina" attempts sign-in to tenant "sign-c" with a wrong password
    And an unknown tenant attempts sign-in as "gina" with the same password
    Then the rejection is indistinguishable from a wrong-password failure

  Scenario: Disabled user sign-in rejected
    Given the operator "harry" with the password "Correct-Operator-0000" exists in tenant "sign-d" named "Sign D Freight" with the initial organization "Sign D HQ"
    And the operator "harry" in tenant "sign-d" is disabled
    When "harry" signs in to tenant "sign-d" with that password
    Then the system rejects the sign-in with the generic rejection

  Scenario: Disabled user refresh rejected
    Given the operator "iris" with the password "Correct-Operator-0000" exists in tenant "sign-e" named "Sign E Freight" with the initial organization "Sign E HQ"
    And a session pair was issued for "iris" in tenant "sign-e"
    And the operator "iris" in tenant "sign-e" is disabled
    When the recorded refresh token is replayed
    Then the refresh is rejected and no new tokens are returned

  Scenario: Refresh succeeds
    Given the operator "jason" with the password "Correct-Operator-0000" exists in tenant "sign-f" named "Sign F Freight" with the initial organization "Sign F HQ"
    And a session pair was issued for "jason" in tenant "sign-f"
    When the recorded refresh token is exchanged
    Then the system returns a new pair of tokens for the same scope

  Scenario: Replay within window returns current pair
    Given the operator "kate" with the password "Correct-Operator-0000" exists in tenant "sign-g" named "Sign G Freight" with the initial organization "Sign G HQ"
    And a session pair was issued and rotated once for "kate" in tenant "sign-g"
    When the invalidated refresh token is replayed immediately
    Then the system returns the current pair without revocation

  Scenario: Replay beyond window revokes family
    Given the operator "leo" with the password "Correct-Operator-0000" exists in tenant "sign-h" named "Sign H Freight" with the initial organization "Sign H HQ"
    And a session pair was issued and rotated once for "leo" in tenant "sign-h"
    When the invalidated refresh token is replayed after the reuse window
    Then the refresh is rejected and the current token is revoked as well

  Scenario: Expired refresh rejected
    Given the operator "mia" with the password "Correct-Operator-0000" exists in tenant "sign-i" named "Sign I Freight" with the initial organization "Sign I HQ"
    And a session pair was issued for "mia" in tenant "sign-i"
    When an unknown refresh token is presented
    Then the refresh is rejected and no token is returned
