import '@sem/UIKit'
import { IntroView } from './IntroView'

@UIApplicationMain class AppDelegate extends UIResponder implements UIApplicationDelegate {

    window? : UIWindow;

    application(application : UIApplication, launchOptions : label<'didFinishLaunchingWithOptions'> | opt<dict<UIApplicationLaunchOptionsKey, any>>) : boolean
    {   
        this.window = new UIWindow(UIScreen.main.bounds)

        let homeViewController = new UIViewController()

        homeViewController.view.addSubview(new IntroView())

        this.window!.rootViewController = homeViewController
        this.window!.makeKeyAndVisible();

        return true;
    }

}