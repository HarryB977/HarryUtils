using System;
using UnityEngine;

namespace HarryUtils.Timer {
    public class Timer {
        // ==================== VARIABLES ===================
        #region Public Variables
        public bool Finished { get; private set; } = false;

        public float ElapsedTime { get; private set; } = 0f;
        public float Duration { get; private set; } = 0f;

        public bool IsInverted { get; private set; } = false;
        #endregion
        
        #region Private Variables
        protected Action callback;
        #endregion
        
        // ==================== METHODS ====================
        public Timer(float _duration, Action _callback = null, bool _inverted = false) {
            Finished = false;

            ElapsedTime = _inverted ? _duration : 0f;
            Duration = _inverted ? 0 : _duration;

            IsInverted = _inverted;

            callback = _callback;
        }

        public Timer WithCallback(Action _callback) {
            callback += _callback;
            return this;
        }
        
        public void Ticks() {
            if (Finished) return;
            
            if (IsInverted) Regressive();
            else Progressive();

            void Progressive() {
                if (Duration > ElapsedTime) ElapsedTime += Time.deltaTime;
                else {
                    ElapsedTime = Duration;
                    Finished = true;

                    callback?.Invoke();
                }
            }

            void Regressive() {
                if (ElapsedTime > 0) ElapsedTime -= Time.deltaTime;
                else {
                    ElapsedTime = 0;
                    Finished = true;

                    callback?.Invoke();
                }
            }
        }

        public void AddDuration(float _duration) {
            Duration += _duration;
        }

        public void TakeDuration(float _duration) {
            Duration -= _duration;
        }
    }
}