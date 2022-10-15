<script setup>
import TheWelcome from '../components/TheWelcome.vue'
</script>

<template>
  <div class="home">
    <h1>This is our home page</h1>
    You can use the below link to log in using KeyCloak, choose to call the protected weather API or visit a front-end protected page:

    <a href="http://localhost:8080" title="Link to login via KeyCloak">Login via KeyCloak</a>
  </div>
</template>
