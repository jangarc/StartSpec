<script setup lang="ts">
import { computed, useSlots } from 'vue';
import { cardClasses, type IamCardProps } from '@startspec/components';

const props = defineProps<IamCardProps>();

const slots = useSlots();

const classes = computed(() => cardClasses(props));
</script>

<template>
  <section :class="classes">
    <div v-if="slots.header || props.title" class="ds-card-header">
      <slot name="header">{{ props.title }}</slot>
    </div>
    <slot />
    <div v-if="slots.footer" class="ds-card-footer">
      <slot name="footer" />
    </div>
  </section>
</template>
