declare interface UIView {
    configureLayout(block : (layout : YGLayout) => void)
    yoga : {
        applyLayout(preservingOrigin : boolean) : void
    }
}

interface YGLayout
{
    isEnabled : boolean
    width : number;
    height : number;
    marginTop : number;
    marginLeft : number;
}