// C++ module binding
import '@sem/stdio.h';

export class Greeter {
    constructor(){}
    greet(name : string, platform : string)
    {
        // declarative C++ API call
        std.printf("\n\n\nHello %s, you're using %s\n\n\n\n", name.c_str(), platform.c_str());
    }
}
