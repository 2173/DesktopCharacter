-- premake5.lua

solution "Live2DforDLL"
    configurations { "Debug", "Release" }
    platforms { "x64", "Any CPU" }
    location ("./")
    startproject "BabumiGraphics"

project "Live2DforDLL"  
    location ("./")
    kind "SharedLib"
    language "C++"
    platforms { "x64" }  
    toolset "v120"
    buildoptions { "/clr" }
    targetdir "$(SolutionDir)..\\Lib\\$(Configuration)\\"
    includedirs { "$(ProjectDir)", "$(SolutionDir)..\\" }
    libdirs { '$(SolutionDir)..\\Lib\\$(Configuration)\\' }
    links { "Live2DWrapping.lib", "opengl32.lib" }
    defines { "_WINDLL", "_MBCS" }
    --dependson {"Generater"}
    removeplatforms { "Any CPU" }
    clr "On"
    
    prebuildcommands {  
        'cd $(SolutionDir)',
        'cd ..\\Generater\\bin\\$(Configuration)',
        'call Generater.exe',
    }

     files {
         "./**.cpp", 
         "./**.h", 
         "./**.config",
     }
    
     excludes  { 
        "./obj/**.*",
        "./bin/**.*",
     }
     configuration { "Debug" }
        buildoptions { "/MDd" }
        