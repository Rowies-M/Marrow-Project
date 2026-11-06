import { useSyncExternalStore } from 'react'

const TOUCH_QUERY = '(pointer: coarse)'

function subscribe(onChange) {
  const media = window.matchMedia(TOUCH_QUERY)
  media.addEventListener('change', onChange)
  return () => media.removeEventListener('change', onChange)
}

function getSnapshot() {
  return window.matchMedia(TOUCH_QUERY).matches
}

export default function useIsTouchDevice() {
  return useSyncExternalStore(subscribe, getSnapshot)
}
