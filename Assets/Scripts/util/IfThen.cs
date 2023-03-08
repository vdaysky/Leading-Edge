using System;

namespace util
{
    public class IfThen {
        private readonly Func<bool> _condition;
        
        public IfThen(Func<bool> condition) {
            _condition = condition;
        }
        
        public static implicit operator bool(IfThen ifThen) {
            return ifThen._condition();
        }
        
        public void Then(Action action) {
            if (this) {
                action();
            }
        }
        
    }
}