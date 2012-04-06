;;; (defmacro name args body)
;;; =>
;;; (fset name `(macro lambda ,args ,body))
(fset 'defmacro
      '(macro lambda (name args expr)
        (list 'fset (list 'quote name)
         (list 'quote
          (list 'macro 'lambda args expr)))))

(defmacro setq (symbol value)
  (list 'set (list 'quote symbol) value))

(defmacro si (condition true false)
  (list 'if condition true false))

(defmacro lambda (args body)
  (list 'quote (list 'lambda args body)))

(defmacro defun (name args body)
  (list 'progn
        (list 'fset (list 'quote name)
              (list 'quote (list 'lambda args body)))
        (list 'quote name)))

(defmacro defvar (name value)
  (list 'setq name value))

(defun atom (x)
  (not (consp x)))

(defun mapcar (f list)
  (if (null list)
      nil
      (cons (funcall f (car list))
            (mapcar f (cdr list)))))

(defun cadr (x)
  (car (cdr x)))

(defmacro let (bindings expr)
  (list '%let
        (mapcar 'car bindings)
        (mapcar 'cadr bindings)
        expr))
