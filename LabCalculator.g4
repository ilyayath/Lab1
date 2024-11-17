grammar LabCalculator;

/*
 * Parser Rules
 */

compileUnit : expression EOF;

expression :
    LPAREN expression RPAREN #ParenthesizedExpr
    | expression EXPONENT expression #ExponentialExpr
    | expression operatorToken=(MULTIPLY | DIVIDE) expression #MultiplicativeExpr
    | expression operatorToken=(ADD | SUBTRACT) expression #AdditiveExpr
    | IDENTIFIER LPAREN argumentList? RPAREN #FunctionCallExpr
    | IDENTIFIER #IdentifierExpr  
    | NUMBER #NumberExpr
    | expression operatorToken=(LESS | GREATER | EQUALS) expression #ComparisonExpr
    | expression MOD expression #ModExpr
    | expression DIV expression #DivExpr
    | NOT expression #NotExpr
    ;

/*
 * Lexer Rules
 */

NUMBER : INT ('.' INT)?; 
IDENTIFIER : [a-zA-Z]+[1-9][0-9]* | 'max' | 'min'; 

INT : ('0'..'9')+;

COMMA: ',';
EXPONENT : '^';
MULTIPLY : '*';
DIVIDE : '/';
SUBTRACT : '-';
ADD : '+';
LESS : '<';
GREATER : '>';
EQUALS : '=';
LPAREN : '(';
RPAREN : ')';
NOT : 'not';
MOD : 'mod';
DIV : 'div';

WS : [ \t\r\n] -> channel(HIDDEN);

argumentList : expression (COMMA expression)*;
