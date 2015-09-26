include brainforth.fs

: red 27 emit ." [31m" ;
: green 27 emit ." [32m" ;
: bold 27 emit ." [1m" ;
: regular 27 emit ." [0m" ;

cr red bold ." EXAMPLE 1 ==============================" cr
            ." on the fly creation and execution of xt" regular cr cr

bf" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++."

green
execute
regular

cr red bold ." EXAMPLE 2 ==============================" cr
            ." putting multiple lines of bf code inside a word" regular cr cr

: my-hello-world
	." Hello from before the big bang!" cr
	[bf-setup]
	[bf]" ++++++++[>++++[>++>+++>+++>"
	[bf]" +<<<<-]>+>+>->>+[<]<-]>>.>-"
	[bf]" --.+++++++..+++.>>.<-.<.+++"
	[bf]" .------.--------.>>+.>++."
	[bf-teardown]
	." Hello from after the heat death of the universe!" cr
;

." This is what my-hello-world does:" cr
green
my-hello-world
regular

cr ." and this is what my-hello-world looks like:" cr
green
see my-hello-world
regular
cr cr
bye
