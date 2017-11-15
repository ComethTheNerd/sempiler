import UIKit
import FlexLayout
import PinLayout
class IntroView: UIView { private var rootFlexContainer: UIView = UIView()
    public required init?(coder _: NSCoder) { fatalError("init(coder:) not supported")
    }

    public required init() { super.init(frame: CGRect.zero)
        var label: UILabel = UILabel()
        label.text = "Hello"
        label.numberOfLines = 0
        var bottomLabel: UILabel = UILabel()
        bottomLabel.text = "World"
        bottomLabel.textColor = UIColor.red
        bottomLabel.numberOfLines = 0
        rootFlexContainer.backgroundColor = UIColor.white
        rootFlexContainer.flex.width(200).alignItems(Flex.AlignItems.center).direction(Flex.Direction.column).padding(12).define({ (_ flex: Flex) in flex.addItem().direction(Flex.Direction.row).define({ (_ flex: Flex) in flex.addItem().direction(Flex.Direction.column).grow(1).shrink(1).define({ (_ flex: Flex) in flex.addItem(label)
            })
            })
            flex.addItem().height(1).marginTop(12).backgroundColor(UIColor.lightGray)
            flex.addItem(bottomLabel).marginTop(12)
        })
        addSubview(rootFlexContainer)
    }

    public override func layoutSubviews() { super.layoutSubviews()
        rootFlexContainer.pin.top().horizontally().margin(safeAreaInsets)
        rootFlexContainer.flex.layout(mode: Flex.LayoutMode.adjustHeight)
    }
}
