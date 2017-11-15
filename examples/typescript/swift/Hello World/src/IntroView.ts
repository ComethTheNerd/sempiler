import '@sem/UIKit'
import '@sem/FlexLayout'
import '@sem/PinLayout'

export class IntroView extends UIView {

    private rootFlexContainer = new UIView();

    constructor(coder : label<'coder'> | NSCoder) : terminator | required
    constructor() : required
    {
        overloads((coder : NSCoder) => {

            fatalError("init(coder:) not supported")

        }, () => {

            super(CGRect.zero);
    
            let label = new UILabel();
            label.text = "Hello";
            label.numberOfLines = 0;
    
            let bottomLabel = new UILabel();
            bottomLabel.text = "World";
            bottomLabel.textColor = UIColor.red;
            bottomLabel.numberOfLines = 0;
    
            this.rootFlexContainer.backgroundColor = UIColor.white;
 
            this.rootFlexContainer.flex.width(200).alignItems(Flex.AlignItems.center).direction(Flex.Direction.column).padding(12).define((flex : Flex) => {
    
                flex.addItem().direction(Flex.Direction.row).define((flex : Flex) => {
    
                    flex.addItem().direction(Flex.Direction.column).grow(1).shrink(1).define((flex : Flex) => {
    
                        flex.addItem(label);
                    })
                })
    
                flex.addItem().height(1).marginTop(12).backgroundColor(UIColor.lightGray);
    
                flex.addItem(bottomLabel).marginTop(12);
            })
       
            this.addSubview(this.rootFlexContainer);
        });
    }

    layoutSubviews() : override | void 
    {
        super.layoutSubviews();

        this.rootFlexContainer.pin.top().horizontally().margin(this.safeAreaInsets)

        this.rootFlexContainer.flex.layout(Flex.LayoutMode.adjustHeight)
    }
}