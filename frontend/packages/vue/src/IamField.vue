<script setup lang="ts">
import { computed } from 'vue';
import { deriveFieldViewModel, type IamFieldProps } from '@startspec/components';

const props = defineProps<IamFieldProps>();

const vm = computed(() => deriveFieldViewModel(props));
</script>

<template>
  <label class="ds-field-label">{{ vm.labelText }}<slot /></label>
  <div v-if="vm.hasError" class="ds-field-alert ds-field-alert-error" role="alert">{{ vm.errorText }}</div>
</template>
