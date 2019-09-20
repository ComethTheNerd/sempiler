
# try to install xcodeproj if missing
`gem list '^xcodeproj$' -i || sudo gem install xcodeproj`
require 'xcodeproj'

# create project from scratch
project = Xcodeproj::Project.new('./alpha.xcodeproj')

# target represents the app artifact being produced by the build
target = project.new_target(:application, 'alpha', :ios, nil, nil, :swift)

# entitlements inject adapted from 
# entitlement_path = 'alpha/alpha.entitlements'

# file = project.new_file(entitlement_path)


attributes = {}
project.targets.each do |target|
    attributes[target.uuid] = {'SystemCapabilities' => { 'com.apple.developer.in-app-payments' => {'enabled' => 1}, } }
    # target.add_file_references([file])
    puts 'Added to target: ' + target.uuid
end
project.root_object.attributes['TargetAttributes'] = attributes


# grouping the emitted files under a folder with the same name as artifact
src = project.new_group('alpha')

res = src.new_group('Resources')


# Note Info.plist is not included in target, but is pointed to by build configuration for target instead
src.new_file('./alpha/Info.plist')

src.new_file('./alpha/Entitlements.plist')


target.source_build_phase.add_file_reference(src.new_file('./alpha/App.swift'))



target.build_configurations.each do |config|
    # Make sure the Info.plist is configured for all configs in the target
    config.build_settings['INFOPLIST_FILE'] = './alpha/Info.plist'
    config.build_settings['PRODUCT_BUNDLE_IDENTIFIER'] = 'com.sempiler'
    config.build_settings['CODE_SIGN_ENTITLEMENTS'] = './alpha/Entitlements.plist'
end

project.save()

`pod install`