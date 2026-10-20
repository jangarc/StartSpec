# Spec Delta

## Purpose

Provides tenant-scoped role assignment and permission evaluation with cross-tenant denial so downstream contexts enforce one consistent access policy. 提供租戶級角色指派與權限判定（含跨租戶拒絕），使下游上下文執行一致的存取政策。

## ADDED Requirements

### Requirement: Role assignment within tenant

The system SHALL assign and revoke roles for users only within the user's own tenant. Assignments referencing a user or role from another tenant SHALL be rejected. 系統 SHALL 僅在使用者所屬租戶內指派與撤銷角色；涉及他租戶使用者或角色的指派 SHALL 拒絕。

#### Scenario: Assign role

- **WHEN** an administrator assigns a role of tenant A to a user of tenant A（管理員將租戶 A 的角色指派給租戶 A 的使用者）
- **THEN** the system records the assignment（系統記錄該指派）

#### Scenario: Cross-tenant assignment rejected

- **WHEN** an administrator assigns a role of tenant A to a user of tenant B（管理員將租戶 A 的角色指派給租戶 B 的使用者）
- **THEN** the system rejects the assignment and records nothing（系統拒絕指派且不記錄）

### Requirement: Permission evaluation

The system SHALL evaluate whether a user holds a given permission in the active tenant and return an allow or deny decision. Users without the required grant SHALL receive deny. 系統 SHALL 判定使用者在作用中租戶是否擁有指定權限並回傳允許或拒絕；未獲授權者 SHALL 收到拒絕。

#### Scenario: Granted permission allows

- **WHEN** the system evaluates a permission granted to the user through an assigned role（系統判定經由已指派角色授予使用者的權限）
- **THEN** the system returns allow（系統回傳允許）

#### Scenario: Missing grant denies

- **WHEN** the system evaluates a permission the user was never granted（系統判定從未授予使用者的權限）
- **THEN** the system returns deny（系統回傳拒絕）

### Requirement: Cross-tenant authorization denial

The system SHALL deny any authorization decision evaluated against a tenant other than the user's own tenant, regardless of role grants. 系統 SHALL 拒絕任何針對使用者所屬租戶以外之租戶的授權判定，無論角色授權為何。

#### Scenario: Cross-tenant evaluation denied

- **WHEN** the system evaluates a user of tenant A against a permission scope of tenant B（系統以租戶 B 的權限範圍判定租戶 A 的使用者）
- **THEN** the system returns deny（系統回傳拒絕）

### Requirement: Revocation propagation within configured bound

The system SHALL propagate role revocation to all service nodes within the configured propagation bound (default 5 seconds). Authorization decisions made after propagation completes SHALL deny the revoked permission. 系統 SHALL 在配置的傳播上限內（預設 5 秒）將角色撤銷推送至所有服務節點；傳播完成後的授權判定 SHALL 拒絕已撤銷的權限。

#### Scenario: Revoked permission denied after propagation

- **WHEN** an administrator revokes a role and propagation completes（管理員撤銷角色且傳播完成）
- **THEN** the system denies subsequent requests requiring that permission on any node（系統在任一節點拒絕後續需該權限的請求）
