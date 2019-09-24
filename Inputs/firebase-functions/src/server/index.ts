// [dho] the Firebase CLI requires that the `firebase-admin` is installed
// so we tell the compiler to add it to the artifact (but we won't bother specifying a version) - 24/09/19
// https://firebase.google.com/docs/admin/setup
#compiler addDependency("firebase-admin");


// [dho] any exported functions in the artifact root (this file)
// are exposed as a server route matching the name of the function (ie. '/hello') - 24/09/19
export function hello() 
{
    return "Hello World!";
}

// [dho] NOTE any functions without parameters (0 arity) are currently served over GET, 
// but functions with parameters are exposed via POST (and the parameters are parsed for you from `req.body`) - 24/09/19