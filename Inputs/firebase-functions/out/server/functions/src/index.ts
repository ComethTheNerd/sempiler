const node_1569356706342_$10 = (()=>{
    return {
    };
})();
const node_1569356706456_$23 = (()=>{
    function hello(){
        return "Hello World!";
    }
    return {
        hello,
    };
})();
exports.api = require("firebase-functions").https.onRequest((()=>{
    const express = require("express");
const cors = require("cors");
const app = express();
app.use(cors({ origin : true }));
    app.get("/hello",async (req,res)=>{
        
try {
    const data = await node_1569356706456_$23.hello();

    res.statusCode = 200;

    res.json({ data : data || void 0 });
} catch (error) {
    res.statusCode = 500;

    res.json({ error: error.message });
}
    });
    return app;
})());
