# Spec Delta

## Purpose

Defines multi-store staffing: one Party holding roles across stores, lazy validity evaluation, transfer as role add/remove, and HQ cross-store visibility. 定義多店人事：同一 Party 兼多店角色、有效期懶惰判定、調店即加減角色、總部跨店可見。

## ADDED Requirements

### Requirement: Multi-store roles per party

A Party SHALL hold roles in multiple stores of the same tenant. Transferring staff SHALL add the new store role and remove the old one; each change is separately auditable. 同一 Party SHALL 兼多店角色；調店 SHALL 新增新店角色並移除舊店角色，每次變更獨立可稽。

#### Scenario: Staff works two stores

- **WHEN** a Party holding a cashier role in store A is assigned cashier in store B（已持 A 店店員角色的 Party 獲派 B 店店員）
- **THEN** the system holds both roles on the same Party（系統在同一 Party 保留兩店角色）

#### Scenario: Transfer moves role

- **WHEN** staff transfers from store A to store B（人員自 A 店調往 B 店）
- **THEN** the system adds the store B role and removes the store A role, each audited（系統新增 B 店角色並移除 A 店角色，各自留痕）

### Requirement: Validity evaluated lazily

Role assignments SHALL carry validity windows (`valid_from/until`). Evaluation SHALL compare against current time and deny expired roles; no sweeper is required. 角色指派 SHALL 帶有效期；判定 SHALL 比對當下時間，逾期即拒絕；無需掃蕩任務。

#### Scenario: Expired role denies

- **WHEN** the system evaluates a role past its `valid_until`（系統判定已逾 `valid_until` 的角色）
- **THEN** the system returns deny without any cleanup job（系統回傳拒絕，無需清理任務）

### Requirement: HQ cross-store visibility

HQ manager roles SHALL see all stores of the tenant; store-scoped roles SHALL see only their own store. Denials across stores follow the identical-response rule. 總部管理角色 SHALL 可見租戶全店；店級角色 SHALL 僅見所屬店；跨店拒絕適用相同回應規則。

#### Scenario: HQ sees all stores

- **WHEN** an HQ manager lists stores（總部管理者列出店鋪）
- **THEN** the system returns every store of the tenant（系統回傳租戶全部店鋪）

#### Scenario: Cashier sees own store only

- **WHEN** a store A cashier requests store B data（A 店店員請求 B 店資料）
- **THEN** the system denies exactly as if it does not exist（系統拒絕，形同資料不存在）
