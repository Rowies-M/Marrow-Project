import { useEffect } from 'react'

const DIRECTIONS = {
  w: 'arriba',
  arrowup: 'arriba',
  s: 'abajo',
  arrowdown: 'abajo',
  a: 'izquierda',
  arrowleft: 'izquierda',
  d: 'derecha',
  arrowright: 'derecha',
}

// Una pulsación equivale a una acción: mantener la tecla apretada no la repite.
export default function useGameKeyboard(enabled, onDirection, onAttack) {
  useEffect(() => {
    if (!enabled) {
      return undefined
    }

    function handleKeyDown(event) {
      if (event.ctrlKey || event.altKey || event.metaKey) {
        return
      }

      if (event.key === ' ') {
        // Sobre un botón, Espacio debe activar ese botón y no atacar.
        if (event.target.closest?.('button, a, input, select, textarea')) {
          return
        }

        event.preventDefault()
        if (!event.repeat) {
          onAttack()
        }
        return
      }

      const direction = DIRECTIONS[event.key.toLowerCase()]
      if (!direction) {
        return
      }

      event.preventDefault()
      if (!event.repeat) {
        onDirection(direction)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [enabled, onDirection, onAttack])
}
