<script setup>
import APIReader from '../components/APIReader.vue'
</script>

<template>
    <div class="api">
        Press the button underneath to perform the API call.
        This call will fail if the current user is not authenticated!
        The component will report on this.
        <APIReader></APIReader>
    </div>
</template>