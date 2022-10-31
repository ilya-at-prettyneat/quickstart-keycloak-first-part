import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', {
    state: () => ({ tokenValue: '' }),
    getters: {
      isLoggedIn: (state) => state.tokenValue && state.tokenValue.length,
      jwtToken: (state) => state.tokenValue
    },
    actions: {
      login(token) {
        // login should not be used as an alternative to logout
        if ( !token || !token.length ) throw('use logout() to reset the token');
        
        //login should also not be used for a logged in user
        if ( this.tokenValue && this.tokenValue.length ) throw('already logged in');

        this.tokenValue = token;
      },
      logout(){
        this.tokenValue = '';
      }
    },
  })