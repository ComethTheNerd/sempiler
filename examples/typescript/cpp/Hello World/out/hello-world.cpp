#include "./hello-world.h"
#include <string>
#include "./Greeter.h"
struct Foo {
 public:
  Foo(){};
  std::string getPlatformName() {
    std::string platformName;
#if _WIN32
    platformName = "WINDOWS";
#elif __APPLE__
    platformName = "MACOSX";
#else
    platformName = "DUNNO";
#endif
    return platformName;
  };
};
int main(int argc, char* argv[]) {
  auto* foo = new Foo();
  Greeter greeter = Greeter();
  greeter.greet("Darius", (*foo).getPlatformName());
  delete foo;
  return 0;
};