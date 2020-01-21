using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using Sempiler.Languages;
using Sempiler.Core;
using Sempiler.Inlining;

namespace Sempiler.Bundling
{
    using static BundlerHelpers;

    public class AndroidBundler : IBundler
    {
        static readonly string[] DiagnosticTags = new string[] { "bundler", "android" };

        public IList<string> GetPreservedDebugEmissionRelPaths(Session session, Artifact artifact, CancellationToken token) => new string[]{};

        public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Shard> shards, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();


            if (artifact.Role != ArtifactRole.Client)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                );

                return result;
            }

            if(shards.Count > 1)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"Artifact '{artifact.Role.ToString()}' does not currently support multiple shards")
                );
            }

            // [dho] TODO FIXUP TEMPORARY HACK - need to add proper support for multiple targets!! - 16/10/19
            var shard = shards[0];

            if(shard.Dependencies.Count > 0)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"'{shard.Role.ToString()}' in artifact '{artifact.Role.ToString()}' does not currently support dependencies")
                );
            }

            var ast = shard.AST;

            var packageIdentifier = PackageIdentifier(artifact);
            var appDirRelPath = $"./app/";
            var srcMainDirRelPath = $"{appDirRelPath}src/main/";
            var packageMainDirRelPath = $"{srcMainDirRelPath}java/{packageIdentifier.Replace('.', '/')}/";

            OutFileCollection ofc = new OutFileCollection();

            var mainActivity = default(ObjectTypeDeclaration);

            // [dho] emit source files - 21/05/19
            {
                var emitter = default(IEmitter);

                if (artifact.TargetLang == ArtifactTargetLang.Java)
                {
                    mainActivity = result.AddMessages(JavaInlining(session, artifact, ast, token));

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    emitter = new JavaEmitter();

                    var emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, shard, ast, token));

                    foreach (var emittedFile in emittedFiles)
                    {
                        ofc[FileSystem.ParseFileLocation($"{packageMainDirRelPath}{emittedFile.Path}")] = emittedFile.Emission;
                    }
                }
                // [dho] TODO Kotlin! - 21/05/19
                else
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                    );
                }

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                // var emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));

                // if (HasErrors(result) || token.IsCancellationRequested) return result;

                // // [dho] putting the source files into the src main folder - 31/05/19
                // // [dho] TODO check if it's actually a source file, because it could be some other kind of file - 31/05/19
                // foreach(var emittedFile in emittedFiles)
                // {
                //     ofc[FileSystem.ParseFileLocation($"{srcMainDirRelPath}{emittedFile.Path}")] = emittedFile.Emission;
                // }
            }

            // [dho] synthesize any requisite files for the target platform - 21/05/19
            {
                // check if has provided explicit manifest in the emitted OutFiles

                // if not then we need to InferManifest(ast, token);
                // create RawOutputFileContent from parsed AndroidManifest

                // do we need the gradle script as well?


                AddRawFileIfNotPresent(ofc, $"{srcMainDirRelPath}AndroidManifest.xml",
// [dho] NOTE ensure no whitespace at start of file or it will be considered invalid - 31/05/19
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    package=""{packageIdentifier}"">

    <application
        android:name="".{artifact.Name}""
        android:allowBackup=""true""
        android:label=""{artifact.Name}"">
        <activity android:name=""{artifact.Name}${ToInlinedObjectTypeClassIdentifier(ast, mainActivity.Node)}"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
            </intent-filter>
        </activity>
    </application>

</manifest>        
");

                // AddCopyOfFileIfMissing(ofc, $"{x}/src/main/java/com/example/myapplication/MainActivity.java", 
                //                             "../Core/Bundling/Android/app/src/main/java/com/example/myapplication/MainActivity.java");

                AddRawFileIfNotPresent(ofc, $"{appDirRelPath}build.gradle",
$@"apply plugin: 'com.android.application'

android {{
    compileSdkVersion 28
    defaultConfig {{
        applicationId ""{packageIdentifier}""
        minSdkVersion 15
        targetSdkVersion 28
        versionCode 1
        versionName ""1.0""
    }}
    buildTypes {{
        release {{
            minifyEnabled false
            proguardFiles getDefaultProguardFile('proguard-android-optimize.txt'), 'proguard-rules.pro'
        }}
    }}
    compileOptions {{
        targetCompatibility = '1.8'
        sourceCompatibility = '1.8'
    }}
}}

dependencies {{
    implementation fileTree(dir: 'libs', include: ['*.jar'])
    // implementation 'com.android.support:appcompat-v7:28.0.0'
    // implementation 'com.android.support.constraint:constraint-layout:1.1.3'
    
    // Litho
    implementation 'com.facebook.litho:litho-core:0.25.0'
    implementation 'com.facebook.litho:litho-widget:0.25.0'

    annotationProcessor 'com.facebook.litho:litho-processor:0.25.0'

    // SoLoader
    implementation 'com.facebook.soloader:soloader:0.5.1'

    // For integration with Fresco
    implementation 'com.facebook.litho:litho-fresco:0.25.0'

    // For testing
    testImplementation 'com.facebook.litho:litho-testing:0.25.0'
}}");

                AddRawFileIfNotPresent(ofc, "local.properties", "sdk.dir=/Users/QuantumCommune/Library/Android/sdk");

                // AddCopyOfFileIfMissing(ofc, ".gitignore", "../Core/Bundling/Android/.gitignore");
                AddRawFileIfNotPresent(ofc, "build.gradle",
@"// Top-level build file where you can add configuration options common to all sub-projects/modules.

buildscript {
    repositories {
        google()
        jcenter()
    }
    dependencies {
        classpath 'com.android.tools.build:gradle:3.4.1'
        
        // NOTE: Do not place your application dependencies here; they belong
        // in the individual module build.gradle files
    }
}

allprojects {
    repositories {
        google()
        jcenter()
    }
}

task clean(type: Delete) {
    delete rootProject.buildDir
}");
                AddRawFileIfNotPresent(ofc, "gradle.properties", "org.gradle.jvmargs=-Xmx1536m");
                AddRawFileIfNotPresent(ofc, "gradlew",
@"

#!/usr/bin/env sh

##############################################################################
##
##  Gradle start up script for UN*X
##
##############################################################################

# Attempt to set APP_HOME
# Resolve links: $0 may be a link
PRG=""$0""
# Need this for relative symlinks.
while [ -h ""$PRG"" ] ; do
    ls=`ls -ld ""$PRG""`
    link=`expr ""$ls"" : '.*-> \(.*\)$'`
    if expr ""$link"" : '/.*' > /dev/null; then
        PRG=""$link""
    else
        PRG=`dirname ""$PRG""`""/$link""
    fi
done
SAVED=""`pwd`""
cd ""`dirname \""$PRG\""`/"" >/dev/null
APP_HOME=""`pwd -P`""
cd ""$SAVED"" >/dev/null

APP_NAME=""Gradle""
APP_BASE_NAME=`basename ""$0""`

# Add default JVM options here. You can also use JAVA_OPTS and GRADLE_OPTS to pass JVM options to this script.
DEFAULT_JVM_OPTS=""""

# Use the maximum available, or set MAX_FD != -1 to use that value.
MAX_FD=""maximum""

warn () {
    echo ""$*""
}

die () {
    echo
    echo ""$*""
    echo
    exit 1
}

# OS specific support (must be 'true' or 'false').
cygwin=false
msys=false
darwin=false
nonstop=false
case ""`uname`"" in
  CYGWIN* )
    cygwin=true
    ;;
  Darwin* )
    darwin=true
    ;;
  MINGW* )
    msys=true
    ;;
  NONSTOP* )
    nonstop=true
    ;;
esac

CLASSPATH=$APP_HOME/gradle/wrapper/gradle-wrapper.jar

# Determine the Java command to use to start the JVM.
if [ -n ""$JAVA_HOME"" ] ; then
    if [ -x ""$JAVA_HOME/jre/sh/java"" ] ; then
        # IBM's JDK on AIX uses strange locations for the executables
        JAVACMD=""$JAVA_HOME/jre/sh/java""
    else
        JAVACMD=""$JAVA_HOME/bin/java""
    fi
    if [ ! -x ""$JAVACMD"" ] ; then
        die ""ERROR: JAVA_HOME is set to an invalid directory: $JAVA_HOME

Please set the JAVA_HOME variable in your environment to match the
location of your Java installation.""
    fi
else
    JAVACMD=""java""
    which java >/dev/null 2>&1 || die ""ERROR: JAVA_HOME is not set and no 'java' command could be found in your PATH.

Please set the JAVA_HOME variable in your environment to match the
location of your Java installation.""
fi

# Increase the maximum file descriptors if we can.
if [ ""$cygwin"" = ""false"" -a ""$darwin"" = ""false"" -a ""$nonstop"" = ""false"" ] ; then
    MAX_FD_LIMIT=`ulimit -H -n`
    if [ $? -eq 0 ] ; then
        if [ ""$MAX_FD"" = ""maximum"" -o ""$MAX_FD"" = ""max"" ] ; then
            MAX_FD=""$MAX_FD_LIMIT""
        fi
        ulimit -n $MAX_FD
        if [ $? -ne 0 ] ; then
            warn ""Could not set maximum file descriptor limit: $MAX_FD""
        fi
    else
        warn ""Could not query maximum file descriptor limit: $MAX_FD_LIMIT""
    fi
fi

# For Darwin, add options to specify how the application appears in the dock
if $darwin; then
    GRADLE_OPTS=""$GRADLE_OPTS \""-Xdock:name=$APP_NAME\"" \""-Xdock:icon=$APP_HOME/media/gradle.icns\""""
fi

# For Cygwin, switch paths to Windows format before running java
if $cygwin ; then
    APP_HOME=`cygpath --path --mixed ""$APP_HOME""`
    CLASSPATH=`cygpath --path --mixed ""$CLASSPATH""`
    JAVACMD=`cygpath --unix ""$JAVACMD""`

    # We build the pattern for arguments to be converted via cygpath
    ROOTDIRSRAW=`find -L / -maxdepth 1 -mindepth 1 -type d 2>/dev/null`
    SEP=""""
    for dir in $ROOTDIRSRAW ; do
        ROOTDIRS=""$ROOTDIRS$SEP$dir""
        SEP=""|""
    done
    OURCYGPATTERN=""(^($ROOTDIRS))""
    # Add a user-defined pattern to the cygpath arguments
    if [ ""$GRADLE_CYGPATTERN"" != """" ] ; then
        OURCYGPATTERN=""$OURCYGPATTERN|($GRADLE_CYGPATTERN)""
    fi
    # Now convert the arguments - kludge to limit ourselves to /bin/sh
    i=0
    for arg in ""$@"" ; do
        CHECK=`echo ""$arg""|egrep -c ""$OURCYGPATTERN"" -`
        CHECK2=`echo ""$arg""|egrep -c ""^-""`                                 ### Determine if an option

        if [ $CHECK -ne 0 ] && [ $CHECK2 -eq 0 ] ; then                    ### Added a condition
            eval `echo args$i`=`cygpath --path --ignore --mixed ""$arg""`
        else
            eval `echo args$i`=""\""$arg\""""
        fi
        i=$((i+1))
    done
    case $i in
        (0) set -- ;;
        (1) set -- ""$args0"" ;;
        (2) set -- ""$args0"" ""$args1"" ;;
        (3) set -- ""$args0"" ""$args1"" ""$args2"" ;;
        (4) set -- ""$args0"" ""$args1"" ""$args2"" ""$args3"" ;;
        (5) set -- ""$args0"" ""$args1"" ""$args2"" ""$args3"" ""$args4"" ;;
        (6) set -- ""$args0"" ""$args1"" ""$args2"" ""$args3"" ""$args4"" ""$args5"" ;;
        (7) set -- ""$args0"" ""$args1"" ""$args2"" ""$args3"" ""$args4"" ""$args5"" ""$args6"" ;;
        (8) set -- ""$args0"" ""$args1"" ""$args2"" ""$args3"" ""$args4"" ""$args5"" ""$args6"" ""$args7"" ;;
        (9) set -- ""$args0"" ""$args1"" ""$args2"" ""$args3"" ""$args4"" ""$args5"" ""$args6"" ""$args7"" ""$args8"" ;;
    esac
fi

# Escape application args
save () {
    for i do printf %s\\n ""$i"" | sed ""s/'/'\\\\''/g;1s/^/'/;\$s/\$/' \\\\/"" ; done
    echo "" ""
}
APP_ARGS=$(save ""$@"")

# Collect all arguments for the java command, following the shell quoting and substitution rules
eval set -- $DEFAULT_JVM_OPTS $JAVA_OPTS $GRADLE_OPTS ""\""-Dorg.gradle.appname=$APP_BASE_NAME\"""" -classpath ""\""$CLASSPATH\"""" org.gradle.wrapper.GradleWrapperMain ""$APP_ARGS""

# by default we should be in the correct project dir, but when run from Finder on Mac, the cwd is wrong
if [ ""$(uname)"" = ""Darwin"" ] && [ ""$HOME"" = ""$PWD"" ]; then
  cd ""$(dirname ""$0"")""
fi

exec ""$JAVACMD"" ""$@""
");

                AddRawFileIfNotPresent(ofc, "gradlew.bat",
@"

@if ""%DEBUG%"" == """" @echo off
@rem ##########################################################################
@rem
@rem  Gradle startup script for Windows
@rem
@rem ##########################################################################

@rem Set local scope for the variables with windows NT shell
if ""%OS%""==""Windows_NT"" setlocal

set DIRNAME=%~dp0
if ""%DIRNAME%"" == """" set DIRNAME=.
set APP_BASE_NAME=%~n0
set APP_HOME=%DIRNAME%

@rem Add default JVM options here. You can also use JAVA_OPTS and GRADLE_OPTS to pass JVM options to this script.
set DEFAULT_JVM_OPTS=

@rem Find java.exe
if defined JAVA_HOME goto findJavaFromJavaHome

set JAVA_EXE=java.exe
%JAVA_EXE% -version >NUL 2>&1
if ""%ERRORLEVEL%"" == ""0"" goto init

echo.
echo ERROR: JAVA_HOME is not set and no 'java' command could be found in your PATH.
echo.
echo Please set the JAVA_HOME variable in your environment to match the
echo location of your Java installation.

goto fail

:findJavaFromJavaHome
set JAVA_HOME=%JAVA_HOME:""=%
set JAVA_EXE=%JAVA_HOME%/bin/java.exe

if exist ""%JAVA_EXE%"" goto init

echo.
echo ERROR: JAVA_HOME is set to an invalid directory: %JAVA_HOME%
echo.
echo Please set the JAVA_HOME variable in your environment to match the
echo location of your Java installation.

goto fail

:init
@rem Get command-line arguments, handling Windows variants

if not ""%OS%"" == ""Windows_NT"" goto win9xME_args

:win9xME_args
@rem Slurp the command line arguments.
set CMD_LINE_ARGS=
set _SKIP=2

:win9xME_args_slurp
if ""x%~1"" == ""x"" goto execute

set CMD_LINE_ARGS=%*

:execute
@rem Setup the command line

set CLASSPATH=%APP_HOME%\gradle\wrapper\gradle-wrapper.jar

@rem Execute Gradle
""%JAVA_EXE%"" %DEFAULT_JVM_OPTS% %JAVA_OPTS% %GRADLE_OPTS% ""-Dorg.gradle.appname=%APP_BASE_NAME%"" -classpath ""%CLASSPATH%"" org.gradle.wrapper.GradleWrapperMain %CMD_LINE_ARGS%

:end
@rem End local scope for the variables with windows NT shell
if ""%ERRORLEVEL%""==""0"" goto mainEnd

:fail
rem Set variable GRADLE_EXIT_CONSOLE if you need the _script_ return code instead of
rem the _cmd.exe /c_ return code!
if  not """" == ""%GRADLE_EXIT_CONSOLE%"" exit 1
exit /b 1

:mainEnd
if ""%OS%""==""Windows_NT"" endlocal

:omega


");

                AddRawFileIfNotPresent(ofc, "settings.gradle", "include ':app'");

                result.Value = ofc;
            }


            return result;
        }

        private static Result<ObjectTypeDeclaration> JavaInlining(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<ObjectTypeDeclaration>();

            var domain = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(domain?.Kind == SemanticKind.Domain);

            var component = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), artifact.Name);

            var packageIdentifier = PackageIdentifier(artifact);

            var packageDecl = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), $"package {packageIdentifier};");

            var importDecls = new List<Node>();

            var applicationClass = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
            {
                // [dho] create name of root class - 01/06/19
                var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), artifact.Name);
                ASTHelpers.Connect(ast, applicationClass.ID, new[] { name.Node }, SemanticRole.Name);

                {
                    // [dho] make the root class public - 01/06/19
                    var publicFlag = NodeFactory.Meta(
                        ast,
                        new PhaseNodeOrigin(PhaseKind.Bundling),
                        MetaFlag.WorldVisibility
                    );

                    ASTHelpers.Connect(ast, applicationClass.ID, new[] { publicFlag.Node }, SemanticRole.Meta);
                }

                {
                    var superType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    var superTypeName = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "android.app.Application");

                    ASTHelpers.Connect(ast, superType.ID, new[] { superTypeName.Node }, SemanticRole.Name);

                    ASTHelpers.Connect(ast, applicationClass.ID, new[] { superType.Node }, SemanticRole.Super);
                }

                {
                    ASTHelpers.Connect(ast, applicationClass.ID, new[] {
                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
@"@Override
public void onCreate() {
    super.onCreate();
    com.facebook.soloader.SoLoader.init(this, false);
}
"
                        ).Node
                    }, SemanticRole.Member);
                }


                {
                    var componentIDsToRemove = new List<string>();
                    var inlinedObjectTypeDecls = new List<Node>();

                    foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(ast, domain.ID))
                    {
                        System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

                        var c = ASTNodeFactory.Component(ast, (DataNode<string>)child);

                        // [dho] every component in the AST (ie. every input file) will be turned into a class and inlined - 01/06/19
                        var r = ConvertToInlinedObjectTypeDeclaration(session, artifact, ast, c, token, ref importDecls);

                        result.AddMessages(r);

                        if (HasErrors(r))
                        {
                            continue;
                        }

                        var staticClass = r.Value;

                        // [dho] is this component the entrypoint for the whole artifact - 21/06/19
                        if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, c))
                        {
                            result.Value = staticClass;
                        }

                        inlinedObjectTypeDecls.Add(staticClass.Node);

                        componentIDsToRemove.Add(child.ID);
                    }

                    // [dho] inline all the existing components as static classes - 01/06/19
                    ASTHelpers.Connect(ast, applicationClass.ID, inlinedObjectTypeDecls.ToArray(), SemanticRole.Member);

                    // [dho] remove the components from the tree because now they have all been inlined - 01/06/19
                    ASTHelpers.DisableNodes(ast, componentIDsToRemove.ToArray());
                }
            }



            // var mainActivityClass = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
            // {
            //     // [dho] create name of root class - 01/06/19
            //     // var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), artifact.Name);
            //     ASTHelpers.Connect(ast, mainActivityClass.ID, new [] { 
            //         NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "MainActivity").Node
            //     }, SemanticRole.Name);

            //     {
            //         // [dho] make the root class public - 01/06/19
            //         var publicFlag = NodeFactory.Meta(
            //             ast,
            //             new PhaseNodeOrigin(PhaseKind.Bundling),
            //             MetaFlag.WorldVisibility
            //         );

            //         ASTHelpers.Connect(ast, mainActivityClass.ID, new [] { publicFlag.Node }, SemanticRole.Meta);
            //     }

            //     {
            //         var superType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
            //         var superTypeName = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "android.app.Activity");

            //         ASTHelpers.Connect(ast, superType.ID, new [] { superTypeName.Node }, SemanticRole.Name);

            //         ASTHelpers.Connect(ast, mainActivityClass.ID, new [] { superType.Node }, SemanticRole.Super);
            //     }

            //     // {
            //     //     var componentIDsToRemove = new List<string>();
            //     //     var inlinedObjectTypeDecls = new List<Node>();

            //     //     foreach(var (child, hasNext) in ASTNodeHelpers.IterateChildren(ast, domain.ID))
            //     //     {
            //     //         System.Diagnostics.Debug.Assert(child.Kind == SemanticKind.Component);

            //     //         var c = ASTNodeFactory.Component(ast, (DataNode<string>)child);

            //     //         // [dho] every component in the AST (ie. every input file) will be turned into a class and inlined - 01/06/19
            //     //         var r = ConvertToInlinedObjectTypeDeclaration(session, artifact, ast, ctid, c, token, ref importDecls);

            //     //         result.AddMessages(r);

            //     //         if(HasErrors(r))
            //     //         {
            //     //             continue;
            //     //         }

            //     //         var staticClass = r.Value;

            //     //         inlinedObjectTypeDecls.Add(staticClass.Node);

            //     //         componentIDsToRemove.Add(child.ID);
            //     //     }

            //     //     // [dho] inline all the existing components as static classes - 01/06/19
            //     //     ASTHelpers.Connect(ast, mainActivityClass.ID, inlinedObjectTypeDecls.ToArray(), SemanticRole.Member);

            //     //     // [dho] remove the components from the tree because now they have all been inlined - 01/06/19
            //     //     ASTHelpers.RemoveNodes(ast, componentIDsToRemove.ToArray());

            //     //     ASTHelpers.Connect(ast, applicationClass.ID, new[] { mainActivityClass.Node }, SemanticRole.Member);
            //     // }
            // }

            // [dho] TODO CLEANUP HACK - 01/06/19
            ASTHelpers.Connect(ast, component.ID, new[] { packageDecl.Node }, SemanticRole.None);

            // [dho] combine the imports - 01/06/19
            {
                // importDecls.Add(NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "import com.facebook.litho.*;").Node);

                ASTHelpers.Connect(ast, component.ID, importDecls.ToArray(), SemanticRole.None);
            }

            // [dho] add the root application class - 17/06/19
            ASTHelpers.Connect(ast, component.ID, new[] { applicationClass.Node }, SemanticRole.None);


            ASTHelpers.Connect(ast, domain.ID, new[] { component.Node }, SemanticRole.Component);


            if (!HasErrors(result))
            {
                // [dho] lithoise the AST - 27/06/19
                var task = new Sempiler.Transformation.AndroidLithoTransformer().Transform(session, artifact, ast, token);

                task.Wait();

                var newAST = result.AddMessages(task.Result);
            
                if(!HasErrors(result) && newAST != ast)
                {
                    result.AddMessages(new Message(MessageKind.Error, "Android Litho Transformer unexpectedly returned a different AST that was discarded"));
                }
            }

            return result;
        }

        private static Result<ObjectTypeDeclaration> ConvertToInlinedObjectTypeDeclaration(Session session, Artifact artifact, RawAST ast, Component component, CancellationToken token, ref List<Node> imports)
        {
            var result = new Result<ObjectTypeDeclaration>();

            var inlinedObjectTypeDecl = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));

            // [dho] create name of class - 01/06/19
            {

                // var sourceWithLocation = ((SourceNodeOrigin)component.Origin).Source as ISourceWithLocation<IFileLocation>;

                // if (sourceWithLocation == null || sourceWithLocation.Location == null)
                // {
                //     result.AddMessages(
                //         new NodeMessage(MessageKind.Error, $"Could not create bundle because component inline indentifier could not be determined", component.Node)
                //         {
                //             Hint = GetHint(component.Node.Origin),
                //             Tags = DiagnosticTags
                //         }
                //     );

                //     return result;
                // }


                // var componentName = component.Name;

                // var relParentDirPath = componentName.Replace(session.BaseDirectory.ToPathString(), "");

                var classIdentifier = default(string);

                // // [dho] is this component the entrypoint for the whole artifact - 01/06/19
                // if (relParentDirPath.ToLower().IndexOf(
                //     $"/{Sempiler.Core.NewCompilerAPI.InferredConfig.SourceDirName}/{artifact.Name}/{Sempiler.Core.NewCompilerAPI.InferredConfig.EntrypointFileName}".ToLower()) == 0)
                // {
                //     classIdentifier = "MainActivity";
                // }
                // else
                // {
                classIdentifier = ToInlinedObjectTypeClassIdentifier(ast, component.Node);
                // }

                var rootClassName = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), classIdentifier);

                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, new[] { rootClassName.Node }, SemanticRole.Name);
            }

            {
                // [dho] make the class static - 01/06/19
                var staticFlag = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Bundling),
                    MetaFlag.Static
                );

                // [dho] make the class public - 01/06/19
                var publicFlag = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Bundling),
                    MetaFlag.WorldVisibility
                );

                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, new[] { staticFlag.Node, publicFlag.Node }, SemanticRole.Meta);
            }

            var inlinerInfo = result.AddMessages(ClientInlining.GetInlinerInfo(session, ast, component.Node, LanguageSemantics.Java, token));

            // [dho] TODO support for this
            foreach(var exportedSymbol in inlinerInfo.ExportedSymbols)
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Could not create bundle because general export clauses are not supported", exportedSymbol)
                    {
                        Hint = GetHint(exportedSymbol.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            {
                var execOnLoadNodes = new Node[inlinerInfo.ExecOnLoads.Count + 2];

                execOnLoadNodes[0] = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "static {").Node;
                {                    int index = 0;
                    foreach(var node in inlinerInfo.ExecOnLoads) execOnLoadNodes[++index] = node; 
                }
                execOnLoadNodes[execOnLoadNodes.Length - 1] = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "}").Node;

                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, execOnLoadNodes, SemanticRole.None);
            }

            if (HasErrors(result) || token.IsCancellationRequested) return result;


            result.AddMessages(
                ProcessImports(session, artifact, ast, component, inlinerInfo.ImportDeclarations, token, ref imports)
            );

            {
                foreach (var namespaceDecl in inlinerInfo.NamespaceDeclarations)
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Could not create bundle because namespaces are not yet supported", namespaceDecl.Node)
                        {
                            Hint = GetHint(namespaceDecl.Node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }


            // [dho] insert the 'entrypoint', the `onCreate` function - 01/06/19
            if (inlinerInfo.Entrypoint != default(Node))
            {
                // [dho] if the component has an entrypoint then make it into an Android Activity,
                // and hook up the entrypoint code - 24/06/19
                {
                    var superType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    var superTypeName = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "android.app.Activity");

                    ASTHelpers.Connect(ast, superType.ID, new[] { superTypeName.Node }, SemanticRole.Name);

                    ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, new[] { superType.Node }, SemanticRole.Super);
                }

                {
                    var onCreateMethodDecl = NodeFactory.MethodDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));

                    {
                        // [dho] make the method public - 01/06/19
                        var publicFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Bundling),
                            MetaFlag.WorldVisibility
                        );

                        ASTHelpers.Connect(ast, onCreateMethodDecl.ID, new[] { publicFlag.Node }, SemanticRole.Meta);
                    }

                    {
                        var overrideAnnotation = NodeFactory.Annotation(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        var overrideOperand = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "Override");

                        ASTHelpers.Connect(ast, overrideAnnotation.ID, new[] { overrideOperand.Node }, SemanticRole.Operand);

                        ASTHelpers.Connect(ast, onCreateMethodDecl.ID, new[] { overrideAnnotation.Node }, SemanticRole.Annotation);
                    }

                    {
                        var name = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "onCreate");

                        ASTHelpers.Connect(ast, onCreateMethodDecl.ID, new[] { name.Node }, SemanticRole.Name);
                    }

                    {
                        var parameter = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "android.os.Bundle savedInstanceState");

                        ASTHelpers.Connect(ast, onCreateMethodDecl.ID, new[] { parameter.Node }, SemanticRole.Parameter);
                    }

                    {
                        var type = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        var typeName = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "void");

                        ASTHelpers.Connect(ast, type.ID, new[] { typeName.Node }, SemanticRole.Name);
                        ASTHelpers.Connect(ast, onCreateMethodDecl.ID, new[] { type.Node }, SemanticRole.Type);
                    }

                    {
                        var body = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Bundling));

                        var content = new List<Node>();

                        var superCall = NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
    @"super.onCreate(savedInstanceState);
final com.facebook.litho.ComponentContext context = new com.facebook.litho.ComponentContext(this);"
    );
                        content.Add(superCall.Node);



                        // [dho] TODO CLEANUP HACK to get function body!! - 01/06/19
                        var userCode = ASTHelpers.GetSingleLiveMatch(ast, inlinerInfo.EntrypointUserCode.ID, SemanticRole.Body);

                        foreach (var explicitExit in LanguageSemantics.Java.GetExplicitExits(session, ast, userCode, token))
                        {
                            if (explicitExit.Kind == SemanticKind.FunctionTermination)
                            {
                                var exitValue = ASTNodeFactory.FunctionTermination(ast, explicitExit).Value;

                                if (exitValue != null)
                                {
                                    // [dho] TODO if(typeOfExpression(returnValue, SemanticKind.ViewConstruction)) - 16/06/19
                                    if (exitValue.Kind == SemanticKind.ViewConstruction)
                                    {
                                        // [dho] if we have an entrypoint expression of the form:
                                        //
                                        // `return <Foo>...</Foo>`
                                        // 
                                        // We need to rewrite it as:
                                        //
                                        // ```
                                        // <Foo>..</Foo>
                                        // node123.build();
                                        // setContentView(LithoView.create(context, node123));
                                        // return;
                                        // ```
                                        //
                                        // Below we will then transform the `ViewConstruction` (ie. `<Foo>...</Foo>`) into code that
                                        // initializes the corresponding `node123` variable in scope.
                                        //
                                        // The net result will be the user can an entrypoint like:
                                        //
                                        // ```
                                        // module.exports = () => {
                                        //
                                        //     ...
                                        //
                                        //     return <Foo>...</Foo>;
                                        // }
                                        // ```
                                        //
                                        // And the compiler will convert the code to the corresponding native Litho code
                                        // and set the Litho view to be displayed when the app launches - 16/06/19

                                        var viewID = exitValue.ID;

                                        ASTHelpers.Replace(ast, explicitExit.ID, new[] {
                                            exitValue,
                                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
    $@"setContentView(com.facebook.litho.LithoView.create(context, {viewID}.build()));
    return;"
                                            ).Node
                                        });


                                        // [dho] this is the code the user put in the default export body - 01/06/19
                                        content.Add(userCode);
                                    }
                                    else
                                    {
                                        result.AddMessages(
                                            new NodeMessage(MessageKind.Error, $"Expected entrypoint return value to be view construction but found '{exitValue.Kind}'", exitValue)
                                            {
                                                Hint = GetHint(exitValue.Origin),
                                                Tags = DiagnosticTags
                                            }
                                        );
                                    }
                                }
                            }
                            else if (explicitExit.Kind == SemanticKind.ViewConstruction)
                            {
                                // [dho] assert that we are dealing with a `() => <X>...</X>` situation - 21/06/19
                                System.Diagnostics.Debug.Assert(inlinerInfo.EntrypointUserCode.Kind == SemanticKind.LambdaDeclaration);

                                // [dho] basically here we are going to go from:
                                //
                                // `() => <View />`
                                //
                                //  to:
                                //
                                //
                                // ````
                                // () => {
                                //    setContentView(com.facebook.litho.LithoView.create(context, <View/>));
                                // }
                                // ```
                                //
                                //  And rely on a subsequent transformation to rewrite the `<View/>` for us - 21/06/19
                                var newLambdaBody = NodeFactory.Block(ast, explicitExit.Origin);
                                {
                                    ASTHelpers.Replace(ast, explicitExit.ID, new[] { newLambdaBody.Node });

                                    // var inv = NodeFactory.Invocation(ast, explicitExit.Origin);
                                    // {
                                    //     ASTHelpers.Connect(ast, inv.ID, new[] { 
                                    //         NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),"setContentView").Node
                                    //     }, SemanticRole.Subject);

                                    //     var arg = NodeFactory.Invocation(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                                    //     {
                                    //         ASTHelpers.Connect(ast, arg.ID, new[] { 
                                    //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),"com.facebook.litho.LithoView.create").Node
                                    //         }, SemanticRole.Subject);

                                    //         ASTHelpers.Connect(ast, arg.ID, new[] { 
                                    //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),"context").Node,
                                    //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),"context").Node, 
                                    //         }, SemanticRole.Argument);
                                    //     }

                                    //     ASTHelpers.Connect(ast, inv.ID, new[] { arg.Node }, SemanticRole.Argument);
                                    // }
                                    var viewID = explicitExit.ID;
                                    ASTHelpers.Connect(ast, newLambdaBody.ID, new[] {
                                        explicitExit,
                                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
                                            $@"setContentView(com.facebook.litho.LithoView.create(context, {viewID}.build()));"
                                        ).Node
                                    }, SemanticRole.Content);
                                }


                                // [dho] this is the code the user put in the default export body - 21/06/19
                                content.Add(newLambdaBody.Node);
                            }
                            else
                            {
                                result.AddMessages(
                                    new NodeMessage(MessageKind.Error, $"Expected entrypoint return value to be view construction but found '{explicitExit.Kind}'", explicitExit)
                                    {
                                        Hint = GetHint(explicitExit.Origin),
                                        Tags = DiagnosticTags
                                    }
                                );
                            }

                        }



                        // [dho] remove original entrypoint construct now we have extracted what
                        // we need from it and transformed the intent - 23/06/19
                        ASTHelpers.DisableNodes(ast, new[] { inlinerInfo.Entrypoint.ID });




                        ASTHelpers.Connect(ast, body.ID, content.ToArray(), SemanticRole.Content);

                        ASTHelpers.Connect(ast, onCreateMethodDecl.ID, new[] { body.Node }, SemanticRole.Body);
                    }

                    ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, new[] { onCreateMethodDecl.Node }, SemanticRole.Member);
                }

            }

            {
                var objectTypes = new Node[inlinerInfo.ObjectTypeDeclarations.Count];

                for (int i = 0; i < objectTypes.Length; ++i) objectTypes[i] = inlinerInfo.ObjectTypeDeclarations[i].Node;

                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, objectTypes, SemanticRole.Member);
            }

            {
                var viewDecls = new Node[inlinerInfo.ViewDeclarations.Count];

                for (int i = 0; i < viewDecls.Length; ++i) viewDecls[i] = inlinerInfo.ViewDeclarations[i].Node;

                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, viewDecls, SemanticRole.Member);
            }

            // [dho] function declarations are not valid in a class 
            // in Java so we have to convert them here - 14/06/19
            {
                foreach (var fnDecl in inlinerInfo.FunctionDeclarations)
                {
                    var methodDecl = result.AddMessages(
                        BundlerHelpers.ConvertToStaticMethodDeclaration(session, ast, fnDecl, token)
                    );

                    // [dho] guard against case when conversion has errored - 14/06/19
                    if (methodDecl != null)
                    {
                        ASTHelpers.Replace(ast, fnDecl.ID, new[] { methodDecl.Node });
                    }
                }
            }

            // // [dho] convert all view constructions to litho views - 14/06/19
            // {
            //     foreach(var view in inlinerInfo.ViewConstructions)
            //     {
            //         result.AddMessages(
            //             ReplaceWithLithoViewConstruction(session, ast, view, token)
            //         );
            //     }
            // }


            // // [dho] convert all view declarations to litho class declarations - 14/06/19
            // {
            //     foreach(var viewDecl in inlinerInfo.ViewDeclarations)
            //     {
            //         result.AddMessages(
            //             ReplaceWithLithoViewDeclaration(session, ast, viewDecl, token)
            //         );
            //     }
            // }


            result.Value = inlinedObjectTypeDecl;

            return result;
        }


        private static Result<object> ProcessImports(Session session, Artifact artifact, RawAST ast, Component component, List<Node> importDeclarations, CancellationToken token, ref List<Node> imports)
        {
            var result = new Result<object>();

            if (importDeclarations?.Count > 0)
            {
                var importsSortedByType = result.AddMessages(
                    ImportHelpers.SortImportDeclarationsByType(session, artifact, ast, component, importDeclarations, LanguageSemantics.Swift, token)
                );

                if (!HasErrors(result) && !token.IsCancellationRequested)
                {
                    foreach (var im in importsSortedByType.SempilerImports)
                    {
                        foreach (var kv in im.ImportInfo.SymbolReferences)
                        {
                            var symbol = kv.Key;
                            var references = kv.Value;

                            switch (symbol)
                            {
                                case CompilerPackageSymbols.View:
                                    {
                                        foreach (var reference in references)
                                        {
                                            // [dho] replace alias with 'Component' usage instead - 22/09/19 (ported : 22/09/19)
                                            ASTNodeHelpers.RefactorName(ast, reference, "Component");
                                        }
                                    }
                                    break;

                                default:
                                    {
                                        var clause = im.ImportInfo.Clauses[symbol];

                                        result.AddMessages(
                                            new NodeMessage(MessageKind.Error, $"Symbol '{symbol}' from package '{im.ImportInfo.SpecifierLexeme}' is a symbolic alias not mapped for {artifact.TargetPlatform}", clause)
                                            {
                                                Hint = GetHint(clause.Origin),
                                                Tags = DiagnosticTags
                                            }
                                        );
                                    }
                                    break;
                            }
                        }

                        // [dho] remove the "sempiler" import because it is a _fake_
                        // import we just use to be sure that the symbols the user refers
                        // to are for sempiler, and not something in global scope for a particular target platform - 24/06/19 (ported : 22/09/19)
                        ASTHelpers.DisableNodes(ast, new[] { im.ImportDeclaration.ID });
                    }

                    foreach (var im in importsSortedByType.ComponentImports)
                    {
                        var importedComponentInlinedName = ToInlinedObjectTypeClassIdentifier(ast, im.Component.Node);

                        result.AddMessages(
                            ImportHelpers.QualifyImportReferences(ast, im, importedComponentInlinedName)
                        );

                        // [dho] remove the import because all components are inlined into the same output file - 24/06/19
                        ASTHelpers.DisableNodes(ast, new[] { im.ImportDeclaration.ID });
                    }

                    foreach (var im in importsSortedByType.PlatformImports)
                    {
                        var specifier = im.ImportDeclaration.Specifier;

                        // [dho] unpack a string constant for the import specifier so it is 
                        // just the raw value of it, because in Java imports are not wrapped in
                        // quotes - 01/06/19 (ported 22/09/19)
                        var newSpecifier = NodeFactory.CodeConstant(
                            ast,
                            specifier.Origin,
                            im.ImportInfo.SpecifierLexeme
                        );

                        ASTHelpers.Replace(ast, specifier.ID, new[] { newSpecifier.Node });

                        imports.Add(im.ImportDeclaration.Node);
                    }
                }
            }

            return result;
        }

    
        private static string ToInlinedObjectTypeClassIdentifier(RawAST ast, Node node)
        {
            var guid = ASTHelpers.GetRoot(ast).ID;

            var sb = new System.Text.StringBuilder(guid + "$$");

            foreach (byte b in System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(node.ID)))
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        struct AndroidManifest
        {
            public Dictionary<string, object> Activities;
            public Dictionary<string, object> Services;
            public Dictionary<string, object> Receivers;
            public Dictionary<string, object> Providers;
            public Dictionary<string, object> Permissions;
        }

        private Result<AndroidManifest> InferManifest(RawAST ast, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        /*
        
        if(artifact.TargetLang == "java")
            {
                var emitter = new Sempiler.Emission.JavaEmitter();

                var outFileCollection = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));

                if(HasErrors(result) || token.IsCancellationRequested) return result;

                if(artifact.TargetPlatform == "android")
                {   
                    var absAndroidSrcMainDirPath = $"{absOutDirPath}/src/main";

                    var manifestFileLocation = FileSystem.ParseFileLocation($"{absAndroidSrcMainDirPath}/AndroidManifest.xml");


                    
                    
                        // import com.android.volley.?
                        //     <uses-permission android:name="android.permission.INTERNET" />

                        // import android.net.ConnectivityManager;
                        // import android.net.NetworkInfo;
                        //     <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />



                        // AndroidManifest 
                        // {
                        //     Dictionary<string, ...> Activities;
                        //     Dictionary<string, ...> Services;
                        //     Dictionary<string, ...> Receivers;
                        //     Dictionary<string, ...> Providers;
                        //     Dictionary<string, ...> Permissions;
                        // }

                        // AndroidManifest InferAndroidManifest(RawAST ast)
                        //     // walks tree and infers the permissions the artifact needs



                    

                        // #compiler add_raw(""); // will just do a copy of a source file to the out dir

                        // #compiler emit_raw("./AndroidManifest.xml", "content here");
                     

                    // [dho] TODO ONLY write a manifest file if the user hasn't already included the source to one - 21/05/19

                    // [dho] TODO actual dynamic manifest information!! - 21/05/19
                    outFileCollection[manifestFileLocation] = new Sempiler.Emission.RawOutFileContent(
@"
<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    package=""pl.czak.minimal"">
    <application android:label=""Minimal"">
        <activity android:name=""MainActivity"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
            </intent-filter>
        </activity>
    </application>
</manifest>                
"
                    );

                    // we need to generate the entrypointtttttttt

                    // generate supporting manifest...
                    // x/
                    // x
                    // x
                    // x
                }
                else if(artifact.TargetPlatform == "awslambda")
                {
                    // we need to generate the entry pointttttttt

                    // we probably need to use an AWSLambda Emitter or modify the AST for the lambda situation...




                    // generate supporting files
                    // x
                    // x
                    // x
                    // x
                    // x
                    // x
                }
         */
    }

}