using System.Collections;
using UnityEngine;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Rulet carkinin GORUNUMU: disk doner, top ters yone doner, yavaslayip istenen sayinin dilimine oturur.
    /// Sonucu SECMEZ; GamblingTable sayiyi secip Spin ile buna verir (SOLID).
    /// Dilimler diskin cocugudur; dilim i, diskin yerel Y acisi i * 360 / 37'dedir (RouletteRules.WheelOrder sirasi).
    /// </summary>
    public class RouletteWheel : MonoBehaviour
    {
        [SerializeField] private Transform disk;
        [SerializeField] private Transform ball;

        [Header("Top")]
        [Tooltip("Top donerken diskin merkezine uzakligi (yerel, metre).")]
        [SerializeField] private float ballOuterRadius = 0.28f;
        [Tooltip("Top dilime oturunca merkeze uzakligi.")]
        [SerializeField] private float ballInnerRadius = 0.22f;
        [SerializeField] private float ballHeight = 0.05f;

        [Header("Donus")]
        [SerializeField] private int diskTurns = 2;
        [SerializeField] private int ballTurns = 4;

        private float diskAngle;
        private float ballAngle;

        private void Awake() => Apply(ballOuterRadius);

        public void Spin(int number, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(SpinRoutine(number, Mathf.Max(0.5f, duration)));
        }

        private IEnumerator SpinRoutine(int number, float duration)
        {
            float startDisk = diskAngle;
            float endDisk = startDisk + 360f * diskTurns + Random.Range(0f, 360f);

            // Top ters yonde en az ballTurns tur atar ve diskteki dilimin acisinda biter.
            float pocket = RouletteRules.PocketIndex(number) * 360f / RouletteRules.PocketCount;
            float startBall = ballAngle;
            float endBall = endDisk + pocket;
            while (endBall > startBall - 360f * ballTurns) endBall -= 360f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                diskAngle = Mathf.Lerp(startDisk, endDisk, eased);
                ballAngle = Mathf.Lerp(startBall, endBall, eased);
                float radius = Mathf.Lerp(ballOuterRadius, ballInnerRadius, Mathf.InverseLerp(0.6f, 1f, t));
                Apply(radius);
                yield return null;
            }

            diskAngle = endDisk;
            ballAngle = endBall;
            Apply(ballInnerRadius);
        }

        private void Apply(float ballRadius)
        {
            if (disk != null) disk.localRotation = Quaternion.Euler(0f, diskAngle, 0f);

            if (ball == null) return;

            float radians = ballAngle * Mathf.Deg2Rad;
            ball.localPosition = new Vector3(Mathf.Sin(radians) * ballRadius, ballHeight, Mathf.Cos(radians) * ballRadius);
        }
    }
}
