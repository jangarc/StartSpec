<script setup lang="ts">
const orgUnit = 'Store Operations';
</script>

<template>
  <main class="shell">
    <header class="shell-header">
      <h1>IAM Store</h1>
      <span class="shell-org">{{ orgUnit }}</span>
    </header>
    <section class="shell-body">
      <p>店家端應用殼（Web 先行；本切片交付工具層與畫面骨架）。</p>
    </section>
  </main>
</template>

<style>
.shell {
  min-height: 100vh;
  background: var(--iam-color-surface);
  color: var(--iam-color-text);
  font-size: var(--iam-font-size-body);
}
.shell-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--iam-space-4);
  padding: var(--iam-space-4) var(--iam-space-6);
  border-bottom: 1px solid var(--iam-color-border);
}
.shell-header h1 {
  font-size: var(--iam-space-6);
  font-weight: var(--iam-font-weight-semibold);
}
.shell-org {
  color: var(--iam-color-text-muted);
  font-size: var(--iam-font-size-caption);
}
.shell-body {
  padding: var(--iam-space-6);
}
</style>
