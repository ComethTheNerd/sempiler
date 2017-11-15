import { Greeter } from './Greeter'

@struct class Foo {
    constructor(){}
    getPlatformName()
    {
        // automatic string handling with std::string
        let platformName : string;

        if(_WIN32 as preproc)
        {
             platformName = 'WINDOWS';
        }
        else if(__APPLE__)
        {
            platformName = 'MACOSX';
        }
        else
        {
            platformName = 'DUNNO'
        }

        return platformName;
    }
}

// function definition
function main(argc : int, argv : ptr<char>[]) : int
{
    // heap allocation
    const foo = heap(new Foo());

    // stack allocation
    const greeter = new Greeter();
    
    // deref pointer
    greeter.greet('Darius', foo.pointee.getPlatformName())

    // delete heap allocated memory
    delete foo.pointee;

    return 0;
}
