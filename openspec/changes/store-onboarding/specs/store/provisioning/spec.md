# Spec Delta

## Purpose

Defines the invitation-plus-review store opening chain: platform-issued invitations, document submission, approval, and store-entity creation with a bound manager account. 定義邀請＋審核開店鏈：平台簽發邀請、店家接受並提交證件、審核通過建店實體與店長帳號。

## ADDED Requirements

### Requirement: Invitation issuance with expiry

The system SHALL let platform operators issue store invitations carrying a single-use token and an expiry bound (`InvitationTtlHours`, default 72). Each invitation targets one brand tenant. 系統 SHALL 允許平台方簽發店家邀請（含一次性 token 與效期上限，預設 72 小時）；每張邀請隸屬單一品牌租戶。

#### Scenario: Issue invitation

- **WHEN** a platform operator invites a prospective store for a brand tenant（平台方為品牌租戶邀請準店家）
- **THEN** the system creates a pending invitation with token and expiry（系統建立待接受邀請，含 token 與效期）

### Requirement: Acceptance with document submission

Acceptance SHALL present a valid unexpired token and submit identity/business documents. Expired, unknown, or reused tokens SHALL be rejected with a generic failure. 接受 SHALL 出示有效未逾期 token 並提交身份/營登證件；逾期、未知或重用 token SHALL 以通用失敗拒絕。

#### Scenario: Accept and submit

- **WHEN** an invitee accepts with a valid token and complete documents（受邀方以有效 token 接受並備齊證件）
- **THEN** the system moves the invitation to pending review（系統將邀請轉為待審核）

#### Scenario: Expired invitation rejected

- **WHEN** acceptance presents an expired or reused token（接受出示逾期或已用 token）
- **THEN** the system rejects with a generic failure and changes nothing（系統以通用失敗拒絕且不變更任何狀態）

### Requirement: Review approval creates store and manager

Approval SHALL create the store entity, bind the manager account to a Party (exact identifier match or new Party), and assign the initial manager role. Rejection SHALL create nothing and record the reason. 核准 SHALL 建立店實體、將店長帳號綁定 Party（精確命中或新建）並授初始店長角色；駁回 SHALL 不建立任何資料並記錄理由。

#### Scenario: Approval opens store

- **WHEN** a reviewer approves a pending-review invitation（審核員核准待審邀請）
- **THEN** the system creates the store, the manager account, and the initial role（系統建立店實體、店長帳號與初始角色）

#### Scenario: Rejection creates nothing

- **WHEN** a reviewer rejects with a reason（審核員駁回並附理由）
- **THEN** the system records the reason and creates no store or account（系統記錄理由，不建店亦不建帳號）
