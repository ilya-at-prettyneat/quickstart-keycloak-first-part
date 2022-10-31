<script>
import { mapStores } from 'pinia'
import { useAuthStore } from '../stores/auth';

export default{
  computed:{
    ...mapStores(useAuthStore)
  },
  mounted(){
    if ( !this.authStore.isLoggedIn ){
      this.$router.push('/?unauthorized=true');
    }
  }
}
</script>

<template>
  <div class="about">
    <h1>🔒 Only authenticated users may see this page</h1>
  </div>
</template>

<style>
@media (min-width: 1024px) {
  .about {
    min-height: 100vh;
    display: flex;
    align-items: center;
  }
}
</style>
