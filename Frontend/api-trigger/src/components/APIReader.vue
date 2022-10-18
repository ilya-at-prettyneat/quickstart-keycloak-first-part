<template>
    <div class="reader">
        <button class="reader__start" type="button" v-if="!isCalling" @click="doCall">Call the API</button>
        <div class="reader__state" v-else>
            <p class="reader__state__string" v-for="s in stateStrings" :key="s">{{s}}</p>
            <pre class="reader__state__outcome" v-if="isSuccess">{{this.resultText}}</pre>
            <div class="reader__state__loader" v-if="isLoading">Loading...</div>
        </div>
    </div>
</template>

<script>
export default {
    data(){
        return {
            isCalling: false,   //is the component performing an API call
            isLoading: false,   //is the component awaiting a result
            isSuccess: false,   //the call yielded a result
            stateStrings: [],    //state strings
            resultText: ''      //text of the result, if one is present
        }
    },
    methods:{
        async doCall(){
            //reset the state
            this.isCalling = true;
            this.stateStrings = [];

            //show loading (yes, just for show)
            this.isLoading = true;
            await new Promise( r => setTimeout(r,400));
            this.isLoading = false;

            //start the call
            this.stateStrings.push('Calling the API...');
            this.isLoading = true;

            //try-catch block for the fetch API
            try {
                this.stateStrings.push('Performing a fetch')
                let results = await fetch('http://localhost:5000/weatherforecast', { mode: 'cors' });

                //turn off loading 
                this.isLoading = false;
                this.stateStrings.push('...completed');

                //call is unauthenticated
                if (results.status == 401){
                    this.stateStrings.push('fetch returned a 401 error: Unauthenticated');
                }
                else
                //call is successful
                if (results.status == 200){
                    console.log('yay');
                    this.isSuccess = true;
                    let weather = await results.json();
                    this.stateStrings.push('Call was successful (status code 200)');
                    for (let cast of weather){
                        this.resultText += `\r\n ${weather.Summary}, ${weather.TemperatureC} degrees.`
                    }
                }
                else
                //other result, unexpected
                {
                    this.stateStrings.push('...the call resulted in a different error. Reload and try again')
                }
            }
            catch(err){
                console.log(err)
                //fetch failed entirely (network error)
                this.stateStrings.push(`...the call resulted in an error: ${err}`)
                this.stateStrings.push('(Verify the URI of the frontend application for CORS issues)')
            }
        }
    }
}
</script>

<style lang="scss" scoped>
    @keyframes loader {
        0%{
            opacity: 1;
        }
        16%{
            opacity: 0;
        }
        33%{
            opacity: 1;
        }
        66%{
            opacity: 0
        }
        100%{
            opacity: 1;
        }
    }

    .reader{
        display: flex;
        width: 100%;
        padding: 3px;

        &__start{
            width: 33%;
            justify-self: center;
            padding: 3px;
            border: 1px solid var(--color-border);
            background: var(--color-background-soft);
            color: var(--color-text);
            border-radius: 5%;
            transition: border-radius 1s ease;
            &:hover{
                border-radius: 15%;
                border: 2px solid var(--color-border-hover);
            }
        }
        &__state{
            width: 100%;
            justify-self: center;
            font-family: monospace;
            padding: 1rem;
            margin: 1rem;
            border: 3px;

            &__string{
                font-weight: bold;
                white-space: pre;                
            }
            &__loader{
                animation: loader 1s ease 0s infinite forwards both;
            }
        }        
    }
</style>