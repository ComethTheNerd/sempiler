#include "./Greeter.h"
#include <stdio.h>
#include <string>
Greeter::Greeter() {}
void Greeter::greet(std::string name, std::string platform) {
  std::printf("\n\n\nHello %s, you're using %s\n\n\n\n", name.c_str(),
              platform.c_str());
};