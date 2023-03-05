using System;
using System.Collections.Generic;


namespace util
{
    
    public class Recharge
    {
        private static readonly Dictionary<object, Dictionary<string, long>> RechargeObjectAbility = new();
        private readonly Dictionary<string, long> _rechargeAbility = new();
        
        public static bool TryAbility(object o, string ability, long cooldown) {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            if (!RechargeObjectAbility.ContainsKey(o)) {
                RechargeObjectAbility.Add(o, new Dictionary<string, long>());
            }
            if (!RechargeObjectAbility[o].ContainsKey(ability)) {
                RechargeObjectAbility[o].Add(ability, 0);
            }

            if (RechargeObjectAbility[o][ability] >= now) 
                return false;
            
            RechargeObjectAbility[o][ability] = now + cooldown;
            return true;
        }

        public bool TryAbility(string ability, long cooldown) {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            if (!_rechargeAbility.ContainsKey(ability)) {
                _rechargeAbility.Add(ability, 0);
            }
            if (_rechargeAbility[ability] >= now) 
                return false; 
            
            _rechargeAbility[ability] = now + cooldown;
            return true;
        }

        public bool IsRecharged(string ability) {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            if (!_rechargeAbility.ContainsKey(ability)) {
                return true;
            }
            return _rechargeAbility[ability] < now;
        }
    }
}