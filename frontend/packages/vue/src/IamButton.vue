<script setup lang="ts">
import { computed } from 'vue';
import { activationSuppressed, buttonClasses, IAM_BUTTON_ACTIVATE_EVENT, type IamButtonProps } from '@startspec/components';

const props = defineProps<IamButtonProps>();

const emit = defineEmits<{ (event: typeof IAM_BUTTON_ACTIVATE_EVENT): void }>();

const classes = computed(() => buttonClasses(props));

function onClick(): void {
  if (activationSuppressed(props)) {
    return;
  }
  emit(IAM_BUTTON_ACTIVATE_EVENT);
}
</script>

<template>
  <button type="button" :class="classes" :disabled="props.disabled" @click="onClick">
    <slot />
  </button>
</template>
