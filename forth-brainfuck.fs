\ code to make forth words out of bf code
\ ---
\ in a forth word definition
\ bf code can be put as such:
\ ramsize [bf-setup] "[bf] ++--<><>][etc" "[bf] ++--<><>][" [bf-teardown]
\ multiple "[bf] ..." blocks can be placed after each other and will
\ execute asif one block as long as they are in the same setup-teardown
\ area
\ compilation is slightly optimized by the fact that stuff like
\ '++++-' would be compiled as '3 bf-data' and
\ corresponding for <> groups
\ [] conditional execution/loop blocks are implemented with BEGIN WHILE REPEAT
\ blocks

\ the counter for stackable operators
variable bfc-counter
\ the state variable for opoerator types
variable bfc-state
0 constant default   \ for [].,
1 constant plusminus \ for + -
2 constant leftright \ for < >
16 chars constant mem-unit

\ the address and length of the source
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

\ change the offset
\ takes care of < > type instructions
\ A+(x & B) will not be out of bounds for any x
: bf-offset ( S A O delta -- S A O )
	chars      \ S A O chardelta
	+          \ S A O+cd
	dup 3 pick >= if \ (O+delta) >= N
		\ S A O
		swap 2 pick mem-unit + \ S O A O+MU
		resize throw           \ S O A
		rot over over +        \ O A S A+S
		mem-unit erase         \ O A S
		mem-unit + -rot swap   \ S A O
	then
;
\ change what is stored at the current offset
\ takes care of + - type instructions
: bf-data ( S A O delta -- S A O )
	>r current-addr r> \ S A O A+O delta
	over c@	+ swap c!  \ S A O A+O delta current
;
\ read a value and store at current offset
\ takes care of the , instruction
: bf-in ( S A O -- S A O )
	current-addr key swap c!
;
\ read data at the current offset and output it
\ takes care of the . instruction
: bf-out ( S A O -- S A O )
	current-addr c@ emit
;

\ the '[' ']' instructions use builtin looping words
: compile-bf-lbrace
	postpone begin
		postpone current-addr postpone c@
		postpone while
;
: compile-bf-rbrace
	postpone repeat
;

\ allocate n characters of memory for
\ the bf program and set up the S A O
\ values
: bf-setup ( n -- S A O )
	mem-unit dup allocate throw \ n a
	2dup swap erase          \ n a
	swap swap 0     \ S A O
;
\ undoes bf-setup's actions
: bf-teardown ( run-time: S A O -- )
	drop nip free throw
;

\ finalizes the stackable operations
: wrapup-stackable
	bfc-state @ case
		plusminus of
			bfc-counter @ ?dup if
				chars postpone literal
				postpone bf-data
			then
		endof
		leftright of
			bfc-counter @ ?dup if
				chars postpone literal
				postpone bf-offset
			then
		endof
	endcase
;

\ check what the state should be after
\ the current instruction
: suggest-state ( c -- d )
	case
		'- of plusminus endof
		'+ of plusminus endof
		'< of leftright endof
		'> of leftright endof
		', of default endof
		'. of default endof
		'[ of default endof
		'] of default endof
		bfc-state @ \ no change for invalid instructions
	endcase
;
: compile-char ( c -- )
	dup suggest-state
	\ if the state changes, we need to wrap up current stacked operations
	dup bfc-state @ <> if \ c suggested-state
		wrapup-stackable
		0 bfc-counter !
		bfc-state !
	else
		drop
	then
	case
		\ stack stackable operations by increasing or decreasing the counter
		'- of bfc-counter dup @ 1- swap ! endof
		'+ of bfc-counter dup @ 1+ swap ! endof
		'< of bfc-counter dup @ 1- swap ! endof
		'> of bfc-counter dup @ 1+ swap ! endof
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

\ save the code address and length in variables
: load-bf-source ( a u -- )
	bfc-source-length !
	bfc-source-addr !
;


\ code to quote + compile bf code into the current word
: [bf]" postpone [ 34 parse load-bf-source compile-bf-source ] ; immediate

: [bf-setup]    bf-setup ;                     \ mostly for symmetry
: [bf-teardown] wrapup-stackable bf-teardown ; \ teardown with wrapup -compile only!

\ code to quote and return an xt for a single line of bf code needs to know the amount of memory needed as well
\ ugly hack
: bf" ( n "bf code" -- xt )
	:noname
		postpone bf-setup postpone [
			34 parse
			load-bf-source
			compile-bf-source
			wrapup-stackable
		] postpone bf-teardown
	postpone ;
;


\ EXAMPLE

: my-hello-world
	." before the big bang" cr
	[bf-setup]
	[bf]" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>"
	[bf]" ---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++."
	[bf-teardown]
	." after the heat death of the universe" cr
;


cr cr ." This is what it does:" cr
my-hello-world

cr cr ." and this is what it looks like:" cr
see my-hello-world

cr cr cr cr


\ bf" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++." execute
