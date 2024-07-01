﻿// note that it expects to load dotnet.js 
// (and wasm files) from _framework folder
import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// get the objects needed to run exported C# code
const { getAssemblyExports, getConfig } =
    await dotnet.create();

// config contains the web-site configurations
const config = getConfig();

// exports contain the methods exported by C#
const exports = await getAssemblyExports(config.mainAssemblyName);

// we call the exported C# method Greeter.JSInteropCallsContainer.Greet
// passing to it an array of names
const text = exports.Greeter.JSInteropCallsContainer.Greet(['Nick', 'Joe', 'Bob']);

// logging the result of Greet method call
console.log(text);

// adding the result of Greet method call to the inner text of 
// an element out id "out"
document.getElementById("out").innerText = text;

export  function greet_() {

    var text2 = exports.Greeter.JSInteropCallsContainer.Greet(['Nick', 'Joe', 'Bob']);
    alert(text2+"aaaaaa");
}

window.test_fun = greet_;

export function greet() {
    alert("Hello from module");
}


export const sampleFunction = () => {
    alert("Hello I'm sample");

}