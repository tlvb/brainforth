\ code to make forth words out of bf code
\ ---
\ in a forth word definition
\ bf code can be put as such:
\ ramsize [bf-setup] [bf]" ++--<><>][etc" [bf]" ++--<><>][" [bf-teardown]
\ multiple [bf]" ..." blocks can be placed after each other and will
\ execute as if one block, as long as they are in the same setup-teardown
\ area
\ compilation is slightly optimized by the fact that stuff like
\ '++++-' would be compiled as '3 bf-data' and
\ corresponding compression of >>>>< groups
\ [] conditional execution/loop blocks are implemented with BEGIN WHILE REPEAT
\ blocks

\ the counter for stackable operators
variable bfc-instruction-counter
\ the state variable for opoerator types
variable bfc-instruction-type
0 constant default   \ for []., -not stackable
1 constant plusminus \ for + -  -stackable
2 constant leftright \ for < >  -stackable
16 chars constant mem-unit

\ the address and length of the source to be compiled
variable bfc-source-addr
variable bfc-source-length

: current-addr ( S A O -- S A O A+O )
	postpone 2dup postpone +
; immediate

\ all during run time the forth the analogs to the bf words all operate
\ on three stack values: S A O ... -- S A O )
\ O is the current offset for
\ A which is the address to the allocated memory chunk and
\ S is the size of the chunk

: increase-mem \ S A O
	swap 2 pick mem-unit + \ S O A O+MU
	resize throw           \ S O A
	rot over over +        \ O A S A+S
	mem-unit erase         \ O A S
	mem-unit + -rot swap   \ S A O
;
\ change the offset                             - target for '<', '>'
\ would-be negative offsets are set to zero
\ offsets > allocated memory will trigger a reallocation
: bf-offset ( S A O delta -- S A O )
	chars      \ S A O chardelta
	+ 0 max    \ S A max(O+cd,0)
	dup 3 pick >= if \ (O+delta) >= N ? -> resize
		increase-mem
	then
;
\ change what is stored at the current offset   - target for '+', '-'
: bf-data ( S A O delta -- S A O )
	>r current-addr r> \ S A O A+O delta
	over c@	+ swap c!  \ S A O
;
\ input a value and store at the current offset - target for ','
: bf-in ( S A O -- S A O )
	current-addr key swap c!
;
\ read data at the current offset and output it - target for '.'
: bf-out ( S A O -- S A O )
	current-addr c@ emit
;

\ head of loop                                  - target for '['
\ requires nonzero value at the current offset to execute
: compile-bf-lbrace
	postpone begin
		postpone current-addr postpone c@
	postpone while
;
\ tail of loop                                  - target for ']'
: compile-bf-rbrace
	postpone repeat
;

\ allocate initial memory for the bf program
\ and set up the S A O values
: bf-setup ( -- S A O )
	mem-unit dup allocate throw \ n a
	2dup swap erase          \ n a
	swap swap 0     \ S A O
;
\ opposite of bf-setup
: bf-teardown ( run-time: S A O -- )
	drop nip free throw
;

\ finalizes the stackable operations
: wrapup-stackable
	bfc-instruction-type @ case
		plusminus of
			bfc-instruction-counter @ ?dup if
				chars postpone literal
				postpone bf-data
			then
		endof
		leftright of
			bfc-instruction-counter @ ?dup if
				chars postpone literal
				postpone bf-offset
			then
		endof
	endcase
;

\ determine the type of the instruction
: determine-instruction-type ( c -- d )
	case
		'+ of plusminus endof
		'- of plusminus endof
		'< of leftright endof
		'> of leftright endof
		', of default endof
		'. of default endof
		'[ of default endof
		'] of default endof
		bfc-instruction-type @ \ no change for invalid instructions
	endcase
;
: compile-char ( c -- )
	dup determine-instruction-type
	\ if the type changes, we need to wrap up current stacked operations
	dup bfc-instruction-type @ <> if \ c suggested-state
		wrapup-stackable
		0 bfc-instruction-counter !
		bfc-instruction-type !
	else
		drop
	then
	case
		\ stack stackable operations by increasing or decreasing the counter
		'- of bfc-instruction-counter dup @ 1- swap ! endof
		'+ of bfc-instruction-counter dup @ 1+ swap ! endof
		'< of bfc-instruction-counter dup @ 1- swap ! endof
		'> of bfc-instruction-counter dup @ 1+ swap ! endof
		\ postpone unstackable simple operations immediately
		', of postpone bf-in endof
		'. of postpone bf-out endof
		\ compile loop operations
		'[ of compile-bf-lbrace endof
		'] of compile-bf-rbrace endof
	endcase
;
: compile-bf-source
	bfc-source-length @ 0 do
		bfc-source-addr @ i chars + c@ compile-char
	loop
;

\ save the code address and length in variables used for compiling
: load-bf-source ( a u -- )
	bfc-source-length !
	bfc-source-addr !
;

\ code to quote + compile bf code into the current word
: [bf]" 34 parse load-bf-source compile-bf-source ; immediate

: [bf-setup]    postpone bf-setup ; immediate  \ mostly for symmetry
: [bf-teardown] wrapup-stackable bf-teardown ; \ teardown with wrapup -compile only!

\ code to quote and return an xt for a single line of bf code needs to know the amount of memory needed as well
: bf" ( n "bf code" -- xt )
	:noname
		postpone bf-setup
		postpone [bf]"
		wrapup-stackable
		postpone bf-teardown
	postpone ;
;

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

bf" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++." execute

regular

cr cr
bye