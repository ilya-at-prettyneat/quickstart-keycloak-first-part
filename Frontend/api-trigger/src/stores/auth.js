import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', {
    state: () => ({ tokenValue: '' }),
    getters: {
      isLoggedIn: (state) => state.tokenValue && state.tokenValue.length,
      jwtToken: (state) => state.tokenValue
    },
    actions: {
      login(token) {
        if ( !token || !token.length ) throw('use logout() to reset the token');
        
        if ( this.tokenValue && this.tokenValue.length ) throw('already logged in');

        this.tokenValue = token;
      },
      logout(){
        this.tokenValue = '';
      }
    },
  })