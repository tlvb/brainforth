include brainforth.fs

: red 27 emit ." [31m" ;
: green 27 emit ." [32m" ;
: bold 27 emit ." [1m" ;
: regular 27 emit ." [0m" ;

cr cr red bold ." EXAMPLE 1 ==============================" regular cr cr

: my-hello-world
	." Hello from before the big bang!" cr
	[bf-setup]
	[bf]" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>"
	[bf]" ---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++."
	[bf-teardown]
	." Hello from after the heat death of the universe!" cr
;

." This is what my-hello-world does:" cr cr
green
my-hello-world
regular

cr cr
." and this is what my-hello-world looks like:" cr
green
see my-hello-world
regular
cr cr red bold ." EXAMPLE 2 ==============================" regular cr cr

green

bf" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++."
execute

regular

cr cr
bye
