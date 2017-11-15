declare module std {
    function printf(format : readonly | (ptr<char> | string), ...args : any[]):  void
}
