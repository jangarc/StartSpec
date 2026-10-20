<script setup lang="ts">
import { reactive, ref } from 'vue';
import '@startspec/components';
import { isAccessUsable } from '@startspec/domain';
import type { SessionTokens, WorkingSession } from '@startspec/domain';
import { createApiAuthPort } from './adapters/apiAuthPort';
import { createFetchHttpPort } from './adapters/httpFetchPort';

interface Outcome {
  tone: 'ok' | 'fail';
  text: string;
}

const ok = (text: string): Outcome => ({ tone: 'ok', text });
const fail = (text: string): Outcome => ({ tone: 'fail', text });
const messageOf = (error: unknown): string => (error instanceof Error ? error.message : 'unknown_error');

function decodeJwtExp(token: string): number {
  const payload = token.split('.')[1] ?? '';
  const normalized = payload.replaceAll('-', '+').replaceAll('_', '/');
  const parsed = JSON.parse(atob(normalized)) as { exp?: number };
  return (parsed.exp ?? 0) * 1000;
}

interface SignedInState {
  userId: string;
  partyId: string;
  tokens: SessionTokens;
  session: WorkingSession;
}

const signedIn = ref<SignedInState | null>(null);

const http = createFetchHttpPort(() => signedIn.value?.tokens.accessToken ?? null);
const auth = createApiAuthPort(http);

const signInForm = reactive({ tenantId: 'hq', loginName: 'admin', password: '' });
const signInOutcome = ref<Outcome | null>(null);
const sessionTenant = ref('');

async function signIn(): Promise<void> {
  try {
    const output = await auth.signIn({ ...signInForm });
    const session: WorkingSession = {
      tenantId: signInForm.tenantId,
      loginName: signInForm.loginName,
      accessExpiresAtMillis: decodeJwtExp(output.accessToken),
    };
    signedIn.value = { userId: output.userId, partyId: output.partyId, tokens: { accessToken: output.accessToken, refreshToken: output.refreshToken }, session };
    sessionTenant.value = signInForm.tenantId;
    signInOutcome.value = ok(`已登入 userId=${output.userId}（作用租戶 ${signInForm.tenantId}，範圍伺服器端解析）`);
  } catch (error) {
    signInOutcome.value = fail(messageOf(error));
  }
}

async function refreshSession(): Promise<void> {
  if (signedIn.value === null) {
    signInOutcome.value = fail('尚未登入');
    return;
  }
  try {
    const tokens = await auth.refresh({ refreshToken: signedIn.value.tokens.refreshToken });
    signedIn.value = { ...signedIn.value, tokens, session: { ...signedIn.value.session, accessExpiresAtMillis: decodeJwtExp(tokens.accessToken) } };
    signInOutcome.value = ok('已刷新工作階段配對');
  } catch (error) {
    signInOutcome.value = fail(messageOf(error));
  }
}

const tenantForm = reactive({ tenantId: '', tenantName: '', initialOrganizationName: '' });
const tenantOutcome = ref<Outcome | null>(null);
const organizations = ref<string[]>([]);

async function provisionTenant(): Promise<void> {
  try {
    const response = await http.send<{ tenantId: string; organizationId: string; title?: string }>({
      method: 'POST',
      path: '/iam/tenants',
      body: {
        tenantId: tenantForm.tenantId,
        tenantName: tenantForm.tenantName,
        initialOrganizationName: tenantForm.initialOrganizationName,
      },
    });
    if (response.status === 201) {
      tenantOutcome.value = ok(`已建立 tenant=${response.body.tenantId} 初始組織=${response.body.organizationId}`);
    } else {
      tenantOutcome.value = fail(response.body.title ?? `HTTP ${response.status}`);
    }
  } catch (error) {
    tenantOutcome.value = fail(messageOf(error));
  }
}

async function loadOrganizations(): Promise<void> {
  try {
    const response = await http.send<{ items: { organizationId: string; name: string }[]; hasNext: boolean; title?: string }>({
      method: 'GET',
      path: '/iam/organizations?limit=50',
    });
    if (response.status === 200) {
      organizations.value = response.body.items.map((item) => item.name);
      tenantOutcome.value = ok(`作用租戶（claims，伺服器端解析）共列出 ${organizations.value.length} 個組織`);
    } else {
      organizations.value = [];
      tenantOutcome.value = fail(response.body.title ?? `HTTP ${response.status}`);
    }
  } catch (error) {
    tenantOutcome.value = fail(messageOf(error));
  }
}

const userForm = reactive({ loginName: '', displayName: '', typeCode: 'id-card', number: '' });
const userOutcome = ref<Outcome | null>(null);

async function provisionUser(): Promise<void> {
  try {
    const response = await http.send<{ userId: string; partyId: string; title?: string }>({
      method: 'POST',
      path: '/iam/users',
      body: {
        loginName: userForm.loginName,
        displayName: userForm.displayName,
        typeCode: userForm.typeCode,
        number: userForm.number,
      },
    });
    if (response.status === 201) {
      userOutcome.value = ok(`已建立（作用租戶 ${sessionTenant.value}）userId=${response.body.userId}`);
    } else {
      userOutcome.value = fail(response.body.title ?? `HTTP ${response.status}`);
    }
  } catch (error) {
    userOutcome.value = fail(messageOf(error));
  }
}

const roleForm = reactive({ name: '', permissionsCsv: '' });
const roleOutcome = ref<Outcome | null>(null);
const assignForm = reactive({ roleId: '', userId: '' });
const assignOutcome = ref<Outcome | null>(null);

async function provisionRole(): Promise<void> {
  try {
    const permissions = roleForm.permissionsCsv.split(',').map((entry) => entry.trim()).filter((entry) => entry.length > 0);
    const response = await http.send<{ roleId: string; title?: string }>({
      method: 'POST',
      path: '/iam/roles',
      body: { name: roleForm.name, permissions },
    });
    if (response.status === 201) {
      assignForm.roleId = response.body.roleId;
      roleOutcome.value = ok(`已建立 roleId=${response.body.roleId}`);
    } else {
      roleOutcome.value = fail(response.body.title ?? `HTTP ${response.status}`);
    }
  } catch (error) {
    roleOutcome.value = fail(messageOf(error));
  }
}

async function assignRole(): Promise<void> {
  try {
    const response = await http.send<{ assignmentId: string; title?: string }>({
      method: 'POST',
      path: '/iam/roles/assignments',
      body: { roleId: assignForm.roleId, userId: assignForm.userId },
    });
    if (response.status === 201) {
      revokeForm.assignmentId = response.body.assignmentId;
      assignOutcome.value = ok(`已指派 assignmentId=${response.body.assignmentId}`);
    } else {
      assignOutcome.value = fail(response.body.title ?? `HTTP ${response.status}`);
    }
  } catch (error) {
    assignOutcome.value = fail(messageOf(error));
  }
}

const evaluationForm = reactive({ permission: '' });
const evaluationOutcome = ref<Outcome | null>(null);
const evaluationResult = ref<null | boolean>(null);

async function evaluatePermission(): Promise<void> {
  try {
    const response = await http.send<{ allowed: boolean; title?: string }>({
      method: 'POST',
      path: '/iam/authorization/evaluations',
      body: { userId: assignForm.userId, permission: evaluationForm.permission },
    });
    if (response.status === 200) {
      evaluationResult.value = response.body.allowed;
      evaluationOutcome.value = ok(`判定完成：allowed=${response.body.allowed}`);
    } else {
      evaluationResult.value = null;
      evaluationOutcome.value = fail(response.body.title ?? `HTTP ${response.status}`);
    }
  } catch (error) {
    evaluationOutcome.value = fail(messageOf(error));
  }
}

const revokeForm = reactive({ assignmentId: '' });
const revokeOutcome = ref<Outcome | null>(null);

async function revokeAssignment(): Promise<void> {
  try {
    const response = await http.send<{ title?: string }>({
      method: 'DELETE',
      path: `/iam/roles/assignments/${revokeForm.assignmentId}`,
    });
    if (response.status === 204) {
      revokeOutcome.value = ok('已撤銷指派（決策面隨 RoleRevoked 事件傳播）');
    } else {
      revokeOutcome.value = fail(response.body.title ?? `HTTP ${response.status}`);
    }
  } catch (error) {
    revokeOutcome.value = fail(messageOf(error));
  }
}
</script>

<template>
  <main class="min-h-screen bg-surface text-text text-body">
    <header class="flex items-center justify-between gap-4 border-b border-border px-6 py-4">
      <h1 class="m-0 text-heading font-semibold">IAM HQ</h1>
      <span class="text-caption text-text-muted">HQ Admin</span>
    </header>

    <section class="my-4 mx-6 grid gap-2 rounded-medium border border-border bg-surface-muted px-6 py-4">
      <h2 class="m-0 text-heading font-semibold">工作階段（sign-in / refresh）</h2>
      <div class="grid gap-2">
        <iam-field label="租戶 ID"><input v-model="signInForm.tenantId" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="登入名稱"><input v-model="signInForm.loginName" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="密碼"><input v-model="signInForm.password" type="password" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
      </div>
      <div class="flex items-center gap-2">
        <iam-button variant="primary" @iam-click="signIn">登入</iam-button>
        <iam-button variant="quiet" @iam-click="refreshSession">刷新權杖</iam-button>
      </div>
      <p v-if="signInOutcome" :class="['m-0', 'text-caption', signInOutcome.tone === 'ok' ? 'text-primary' : 'text-danger']">
        {{ signInOutcome.text }}
      </p>
      <ul v-if="signedIn" class="m-0 grid gap-2 p-0">
        <li>userId: {{ signedIn.userId }}</li>
        <li>partyId: {{ signedIn.partyId }}</li>
        <li>
          權杖到期: {{ new Date(signedIn.session.accessExpiresAtMillis).toISOString() }}（
          {{ isAccessUsable(signedIn.session, Date.now()) ? '可用' : '已逾期' }}）
        </li>
        <li>作用租戶（範圍自驗證 claims 解析，不送租戶標頭）: {{ sessionTenant }}</li>
      </ul>
    </section>

    <section class="my-4 mx-6 grid gap-2 rounded-medium border border-border bg-surface-muted px-6 py-4">
      <h2 class="m-0 text-heading font-semibold">租戶（platform entry provision / organizations）</h2>
      <div class="grid gap-2">
        <iam-field label="新租戶 ID"><input v-model="tenantForm.tenantId" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="新租戶名稱"><input v-model="tenantForm.tenantName" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="初始組織名稱"><input v-model="tenantForm.initialOrganizationName" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
      </div>
      <div class="flex items-center gap-2">
        <iam-button variant="primary" @iam-click="provisionTenant">建立租戶</iam-button>
        <iam-button variant="quiet" @iam-click="loadOrganizations">列出組織（claims 範圍）</iam-button>
      </div>
      <p v-if="tenantOutcome" :class="['m-0', 'text-caption', tenantOutcome.tone === 'ok' ? 'text-primary' : 'text-danger']">
        {{ tenantOutcome.text }}
      </p>
      <ul v-if="organizations.length > 0" class="m-0 grid gap-2 p-0">
        <li v-for="name in organizations" :key="name">{{ name }}</li>
      </ul>
    </section>

    <section class="my-4 mx-6 grid gap-2 rounded-medium border border-border bg-surface-muted px-6 py-4">
      <h2 class="m-0 text-heading font-semibold">使用者驗證（user provision，作用租戶＝工作階段租戶）</h2>
      <div class="grid gap-2">
        <iam-field label="登入名稱"><input v-model="userForm.loginName" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="顯示名稱"><input v-model="userForm.displayName" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="證件類型"><input v-model="userForm.typeCode" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="證件號碼"><input v-model="userForm.number" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
      </div>
      <div class="flex items-center gap-2">
        <iam-button variant="primary" @iam-click="provisionUser">建立使用者</iam-button>
      </div>
      <p v-if="userOutcome" :class="['m-0', 'text-caption', userOutcome.tone === 'ok' ? 'text-primary' : 'text-danger']">
        {{ userOutcome.text }}
      </p>
    </section>

    <section class="my-4 mx-6 grid gap-2 rounded-medium border border-border bg-surface-muted px-6 py-4">
      <h2 class="m-0 text-heading font-semibold">授權（role / assign / evaluate / revoke，作用租戶＝工作階段租戶）</h2>
      <div class="grid gap-2">
        <iam-field label="角色名稱"><input v-model="roleForm.name" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="權限（逗號分隔）"><input v-model="roleForm.permissionsCsv" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="roleId"><input v-model="assignForm.roleId" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="userId"><input v-model="assignForm.userId" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label="評估權限 key"><input v-model="evaluationForm.permission" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
        <iam-field label=" assignmentId（撤銷用）"><input v-model="revokeForm.assignmentId" class="w-full rounded-small border border-border bg-surface px-2 py-2 text-body text-text" /></iam-field>
      </div>
      <div class="flex items-center gap-2">
        <iam-button variant="primary" @iam-click="provisionRole">建立角色</iam-button>
        <iam-button variant="primary" @iam-click="assignRole">指派角色</iam-button>
        <iam-button variant="quiet" @iam-click="evaluatePermission">評估權限</iam-button>
        <iam-button variant="danger" @iam-click="revokeAssignment">撤銷指派</iam-button>
      </div>
      <p v-if="roleOutcome" :class="['m-0', 'text-caption', roleOutcome.tone === 'ok' ? 'text-primary' : 'text-danger']">{{ roleOutcome.text }}</p>
      <p v-if="assignOutcome" :class="['m-0', 'text-caption', assignOutcome.tone === 'ok' ? 'text-primary' : 'text-danger']">{{ assignOutcome.text }}</p>
      <p v-if="evaluationOutcome" :class="['m-0', 'text-caption', evaluationOutcome.tone === 'ok' ? 'text-primary' : 'text-danger']">
        {{ evaluationOutcome.text }}
      </p>
      <p v-if="revokeOutcome" :class="['m-0', 'text-caption', revokeOutcome.tone === 'ok' ? 'text-primary' : 'text-danger']">{{ revokeOutcome.text }}</p>
    </section>
  </main>
</template>
