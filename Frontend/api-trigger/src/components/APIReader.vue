<template>
    <div class="reader">
        <button class="reader__start" type="button" v-if="!isCalling" @click="doCall">Call the API</button>
        <div class="reader__state" v-else>
            <p class="reader__state__string">{{ stateString }}</p>
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
            stateString: '',    //state string
            resultText: ''      //text of the result, if one is present
        }
    },
    methods:{
        async doCall(){
            //reset the state
            this.isCalling = true;
            this.stateString = '';

            //show loading
            this.isLoading = true;
            await new Promise( r => setTimeout(r,400));
            this.isLoading = false;

            //start the call
            this.stateString = 'Calling the API...';
            this.isLoading = true;

            //try-catch block for the fetch API
            try {
                let results = await fetch('http://localhost:5000/weatherforecast');

                //turn off loading 
                this.isLoading = false;

                //call is unauthenticated
                if (results.status == 401){
                    this.stateString += '\r\n ...the call resulted in a 401 (Unautheticated user). Is the user authenticated?';
                }
                else
                //call is successful
                if (results.status == 200){
                    this.isSuccess = true;
                    let weather = await results.json();
                    this.stateString += '\r\n...call successful:';
                    for (let cast of weather){
                        this.resultText += `\r\n ${weather.Summary}, ${weather.TemperatureC} degrees.`
                    }
                }
                else
                //other result, unexpected
                {
                    this.stateString += '\r\n ...the call was unsuccessful. Reload and retry'
                }
            }
            catch(err){
                //fetch failed entirely (network error)
                this.stateString += `\r\n ...the call was unsuccessful: ${err}`
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
            border: 1px solid rgba(0,0,0,0.3);
            border-radius: 5%;
            transition: border-radius 1s ease;
            &:hover{
                border-radius: 8%;
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