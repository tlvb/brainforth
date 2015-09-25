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

\ the address and length of the source
variable bfc-source-addr
variable bfc-source-length

: current-addr ( B A O -- B A O A+O )
	postpone 2dup postpone +
; immediate

\ all during run time the forth the analogs to the bf words all operate
\ on three stack values: B A O ... -- B A O )
\ O is the current offset for
\ A which is the allocated memory chunk
\ where B is the upper bound so that

\ change the offset
\ takes care of < > type instructions
\ A+(x & B) will not be out of bounds for any x
: bf-offset ( B A O delta -- B A O )
	chars      \ B A O chardelta
	+          \ B A O+cd
	2 pick and \ B A (O+delta)&B
;
\ change what is stored at the current offset
\ takes care of + - type instructions
: bf-data ( B A O delta -- B A O )
	>r current-addr r> \ B A O A+O delta
	over c@	+ swap c!    \ B A O A+O delta current
;
\ read a value and store at current offset
\ takes care of the , instruction
: bf-in ( B A O -- B A O )
	current-addr key swap c!
;
\ read data at the current offset and output it
\ takes care of the . instruction
: bf-out ( B A O -- B A O )
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
\ the bf program and set up the B A O
\ values
: bf-setup ( n -- B A O )
	dup chars allocate throw \ n a
	2dup swap erase          \ n a
	swap 1- chars swap 0     \ B A O
;
\ undoes bf-setup's actions
: bf-teardown ( run-time: B A O -- )
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
	>r :noname r> \ >r> to sneak the memory requirements past the colon-sys
		postpone literal postpone bf-setup postpone [
			34 parse
			load-bf-source
			compile-bf-source
			wrapup-stackable
		] postpone bf-teardown
	postpone ;
;


\ EXAMPLE

: my-hello-world
	8
	[bf-setup]
	[bf]" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++."
	[bf-teardown]
;


cr cr ." This is what it does:" cr
my-hello-world

cr cr ." and this is what it looks like:" cr
see my-hello-world

cr cr cr cr


\ 8 bf" ++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++." execute
