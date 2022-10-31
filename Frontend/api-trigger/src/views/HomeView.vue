<script setup>
import {ref, onMounted} from 'vue';
import {useRoute} from 'vue-router';
import { useAuthStore } from '../stores/auth';

const fromProtected = ref(false);
const route = useRoute();
const authStore = useAuthStore();

onMounted(async () => {
  //if redirection happened here with a note about being unauthorized, set the reactive value
  if ( route.query.unauthorized && route.query.unauthorized=='true')
    fromProtected.value = true;

  //get token via oauth
  if ( route.query.code && route.query.code.length ){
    let outcome = await fetch(`http://localhost:5000/oauth?code=${route.query.code}`)
    // request went well
    if ( outcome.ok ){
      let data = await outcome.json();
      // call the action in the store to set the token and be "isLoggedIn"
      authStore.login(data.token);
    }
  }
})
</script>

<template>
  <div class="home">
    <h1>This is our home page</h1>
    <div v-if="!authStore.isLoggedIn">
      <p>You can use the below link to log in using KeyCloak, choose to call the protected weather API or visit a front-end protected page:</p>

<a href="http://localhost:8080/realms/test/protocol/openid-connect/auth?client_id=account&redirect_uri=http://127.0.0.1:5173/oauth&response_type=code&scope=openid" title="Link to login via KeyCloak">Login via KeyCloak</a>      
    </div>
    <div v-else>
      You are logged in! Try the about page or the API!
    </div>
  </div> 
  <div class="authorization-error" v-if="fromProtected">You tried accessing a protected page! Log in first.</div>
</template>
