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
